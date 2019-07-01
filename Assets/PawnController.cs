using UnityEngine;
using System.Collections;

using Mirror;
using System.Collections.Generic;

public class PawnController : NetworkBehaviour {

	private AudioSource audioSource;

	#region Client Side
	private BaseSlotController baseSlotController;
	#endregion

	#region Server Side
	private bool atFinish;
	#endregion

	private int currentTileIndex; // order index
	private int startTileIndex = -1;
	private int lastTileIndex = -1;
	private bool inner;
	private bool staged; // in base

	[SyncVar] public bool canSelect;
	public bool isSelected;

	private GameController gci = null; // GameController Instance

	// ======================================================================================

	[SyncVar] private bool render;
	public PlayerController playerController = null;
	private int slot = -1;
	private bool isMoving = false;

	// Animation START
	private Transform sphere;
	private Vector3 originalScale;
	// Animation END

	private BoxCollider boxCollider;


	//=============
	[SyncVar] private Vector3 position;
	[SyncVar] public int sphereMaterial;

	private void Awake() {
		gci = GameController.Instance;
		boxCollider = transform.GetChild(0).GetComponent<BoxCollider>();
		sphere = transform.GetChild(1);
		originalScale = sphere.localScale;
		audioSource = GetComponent<AudioSource>();
	}

	private void Start() {
		StartCoroutine(SelectAnimation());
	}

	[Server]
	public bool SetCanSelect(int rolledAmount) {
		if (atFinish) {
			canSelect = false;
		} else {
			if (staged && rolledAmount == 6) {
				canSelect = true;
			} else if (!staged && !inner) {
				canSelect = true;
			} else if (inner && (currentTileIndex + rolledAmount) <= 5) { // same as IsSelectable
				canSelect = true;
			} else {
				canSelect = false;
			}
		}

		return canSelect;
	}

	public bool IsSelectable() => (!inner || (inner && currentTileIndex == lastTileIndex)) && !atFinish;

	[Server]
	public bool OnSelect() {
		if (canSelect) {
			if (!isSelected) {
				return isSelected = true;
			}
		}

		return false;
	}

	private bool scaleDirection;

	private IEnumerator SelectAnimation() {
		while (true) {
			if (canSelect && gci.currentTurn == slot) {
				Vector3 step = Vector3.one / 10;

				if (scaleDirection) {
					sphere.localScale += step;

					if (sphere.localScale.x >= 5f) {
						scaleDirection = false;
					}
				} else {
					sphere.localScale -= step;

					if (sphere.localScale.x <= 1f) {
						scaleDirection = true;
					}
				}
			} else {
				if (sphere.localScale != originalScale) {
					sphere.localScale = originalScale;
				}
			}

			yield return new WaitForSeconds(0.005f);
		}
	}

	private void FixedUpdate() {
		if (isServer) {
			if (playerController) {
				if (playerController.rolledAmount > 0) {
					ServerMove();
				}
			}
		}
	}

	[Server]
	public void ServerMove() {
		if (!atFinish) {
			if (isSelected && !isMoving) { // prevent from server spam even if at finish
				isMoving = true;
				StartCoroutine(Move(playerController.rolledAmount));
			}
		}
	}

	[Server]
	private IEnumerator Move(int rolledAmount) {
		int destinationTileIndex = -1;

		if (staged) {
			if (rolledAmount == 6) {
				rolledAmount = 0;
				staged = false;
				destinationTileIndex = startTileIndex;
				TargetMoveToStart(playerController.connectionToClient);
			}
		} else {
			while (rolledAmount > 0) {
				rolledAmount--;

				if ((currentTileIndex + 1) == gci.numberOfTiles) {
					currentTileIndex = 0;
					destinationTileIndex = currentTileIndex;
				} else if (currentTileIndex == lastTileIndex) {
					inner = true;
					currentTileIndex = -1; // just entered
					rolledAmount++;
					destinationTileIndex = lastTileIndex;
					//yield return false;
					continue;
				} else if (inner) {
					Debug.Log("currentTileIndex: " + currentTileIndex + ", rolledAmount: " + rolledAmount + ", total: " + (currentTileIndex + rolledAmount));
					if ((currentTileIndex + rolledAmount) > 5) {
						break;
						//} else if (currentTileIndex != 5 && (rolledAmount + 1) > 0) { // prevents 5 + 0, OOB Exception
					} else if (currentTileIndex != 5 && (rolledAmount >= 0)) {
						currentTileIndex++;

						atFinish = gci.player[slot].innerPath[currentTileIndex].tag == "Finish";

						if (atFinish) {
							Debug.Log("=== BONUS MOVE GRANTED DUE TO LANDING ON FINISH ===");
							playerController.rollsLeft++;
						}
					}
				} else {
					currentTileIndex++;
					destinationTileIndex = currentTileIndex;
				}

				TargetMove(playerController.connectionToClient, currentTileIndex, inner);

				yield return false;
			}
		}

		bool bonusEliminationMove = false;

		// Move is finished and rolledAmount is zero, if collides with other pawns
		if (!inner && rolledAmount == 0 && (!inner || currentTileIndex != lastTileIndex)) {
			Debug.Log("DestinationTileIndex: " + destinationTileIndex);

			GameObject destinationTile = gci.path[destinationTileIndex];

			// If a piece lands upon a piece of the same colour, this forms a block.This block cannot be passed or landed on by any opposing piece.

			switch (destinationTile.tag) {
				case "Start":
				case "Finish":
				case "Safe":
					break;
				default:
					Collider[] hitColliders = Physics.OverlapBox(destinationTile.transform.position, boxCollider.size / 2f, Quaternion.identity);

					Debug.Log(hitColliders.Length);

					int[] pawnsOnTile = new int[GameController.Instance.bases.Count];

					foreach (Collider collider in hitColliders) {
						GameObject hitGameObject = collider.transform.parent.gameObject;
						PawnController hitPawnController = hitGameObject.GetComponent<PawnController>();

						if (hitPawnController != this) {
							Debug.Log(collider.transform.parent.gameObject);
							pawnsOnTile[hitPawnController.slot] += 1;
						}
					}

					List<int> toEliminate = new List<int>();

					//if (pawnsOnTile[slot] >= 1) { // build a block
						// eliminate any other single pawns

					for (int i = 0; i < pawnsOnTile.Length; ++i) {
						if (i == slot) {
							continue;
						}

						int pawnCount = pawnsOnTile[i];

						if (pawnCount == 1) {
							toEliminate.Add(i);
						}
					}
					//}

					foreach (int slot in toEliminate) {
						foreach (Collider collider in hitColliders) {
							GameObject hitGameObject = collider.transform.parent.gameObject;
							PawnController hitPawnController = hitGameObject.GetComponent<PawnController>();

							if (slot == hitPawnController.slot) {
								hitPawnController.Eliminated();
								bonusEliminationMove = true;
							}
						}
					}

					break;
			}
		}

		if (bonusEliminationMove) {
			playerController.rollsLeft++;
		}
		
		playerController.pawnSelected = isSelected = isMoving = false;
		playerController.rolledAmount = 0;
	}

	// store moves in a queue, wait second each before moving.
	// whenever is done let the server know?

	[Server]
	private void Eliminated() {
		// atbase
		// currentTileIndex

		staged = true;
		currentTileIndex = startTileIndex;

		// set something????
		TargetMoveToBase(playerController.connectionToClient);

		int randomSound = Random.Range(0, gci.sounds.Length);

		RpcEliminated(randomSound);
	}

	[ClientRpc]
	private void RpcEliminated(int index) {
		audioSource.PlayOneShot(gci.sounds[index]);
	}

	[TargetRpc]
	private void TargetMove(NetworkConnection connection, int location, bool inner) {
		Vector3 position;

		if (inner) {
			Debug.Log("slot: " + slot + ", location: " + location);
			position = gci.player[slot].innerPath[location].position;
		} else {
			position = gci.path[location].transform.position;
		}

		transform.position = position;
		CmdMove(transform.position);
	}

	[Server]
	public void Init(PlayerController playerController) {
		if (this.playerController == null) {
			this.playerController = playerController;
			staged = true;
			slot = playerController.slot;
			SetMaterial();
			GameController.Player player = GameController.Instance.player[slot];
			currentTileIndex = startTileIndex = player.startTileIndex;
			lastTileIndex = player.lastTileIndex;

			if (isServer) {
				RpcInit(playerController.slot, playerController.netId);
			}
		}
	}

	[ClientRpc]
	public void RpcInit(int slot, uint playerControllerNetId) {
		if (playerController == null) {
			if (NetworkIdentity.spawned.TryGetValue(playerControllerNetId, out NetworkIdentity playerControllerNetworkIdentity)) {
				this.slot = slot;
				SetMaterial();
				playerController = playerControllerNetworkIdentity.gameObject.GetComponent<PlayerController>();
				GameController.Player player = GameController.Instance.player[slot];
				currentTileIndex = startTileIndex = player.startTileIndex;
				lastTileIndex = player.lastTileIndex;
			}
		}
	}

	public override void OnStartClient() {
		transform.position = position;
		SetMaterial();
		EnableRendering(render);
	}

	private void SetMaterial() {
		sphere.gameObject.GetComponent<MeshRenderer>().material = gci.materials[sphereMaterial];
	}

	[Command]
	private void CmdEnableRendering(bool status) {
		render = status;
		EnableRendering(status);
		RpcEnableRendering(status);
	}

	[ClientRpc]
	private void RpcEnableRendering(bool status) {
		EnableRendering(status);
	}

	private void EnableRendering(bool status) {
		sphere.gameObject.GetComponent<MeshRenderer>().enabled = status;
	}

	[Command]
	private void CmdMove(Vector3 position) { // rework, send index. will work for now
		this.position = transform.position = position;
		RpcMove(position);
	}

	[ClientRpc]
	private void RpcMove(Vector3 position) {
		transform.position = position;
	}

	[TargetRpc]
	public void TargetMoveToBase(NetworkConnection connection) {
		for (int i = 0; i < gci.bases[slot].transform.childCount; ++i) {
			Transform childTransform = gci.bases[slot].transform.GetChild(i);

			baseSlotController = childTransform.GetComponent<BaseSlotController>();
			if (!baseSlotController.occupied) {
				transform.position = childTransform.position;

				//EnableRendering(true);

				if (hasAuthority) {
					CmdEnableRendering(true);
				}

				staged = true;
				baseSlotController.occupied = true;
				currentTileIndex = startTileIndex;
				CmdMove(transform.position);
				break;
			}
		}
	}

	[TargetRpc]
	private void TargetMoveToStart(NetworkConnection connection) {
		baseSlotController.occupied = false;
		transform.position = gci.path[startTileIndex].transform.position;
		CmdMove(transform.position);
	}

}
