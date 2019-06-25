using UnityEngine;
using System.Collections;

using Mirror;

public class PawnController : NetworkBehaviour {

	private int currentTileIndex; // order index
	private int startTileIndex = -1;
	private int lastTileIndex = -1;
	private bool inner;
	private bool atBase;
	private bool atFinish;

	public bool canSelect;
	public bool isSelected;

	private Vector3 originalScale;

	private GameController gci = null; // GameController Instance

	// TEMP

	private int slot = -1;

	// ======================================================================================

	[SyncVar] private bool render;

	private void Awake() {
		gci = GameController.Instance;
		originalScale = transform.localScale;
	}

	private void Start() {
		StartCoroutine(SelectAnimation());
	}

	// MOVE ELSEWHERE, CHECK IF SELECTED AFTER DIE WAS ROLLED
	// Do not allow to select if inner && location == 5
	private void Update() {
		if (hasAuthority) {
			//if (!gci.pawnSelected && gci.currentTurn == assignedSlot) {
			//	canSelect = true;
			//} else {
			//	canSelect = false;
			//}
		}
	}

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
			if (canSelect) {
				Vector3 step = Vector3.one / 10;

				if (scaleDirection) {
					transform.localScale += step;

					if (transform.localScale.x >= 5f) {
						scaleDirection = false;
					}
				} else {
					transform.localScale -= step;

					if (transform.localScale.x <= 1f) {
						scaleDirection = true;
					}
				}
			} else {
				if (transform.localScale != originalScale) {
					transform.localScale = originalScale;
				}
			}

			yield return new WaitForSeconds(0.005f);
		}
	}

	[Server]
	public void ServerMove() {

		if (!atFinish && isSelected) { // prevent from server spam even if at finish

		}

		//if (!atFinish) { // prevent from server spam even if at finish
		//	Debug.Log("move)");
		//	StartCoroutine(Move(GameController.Instance.RollDice()));
		//}
	}

	[Server]
	private IEnumerator Move(int rolledAmount) {
		if (atBase) {
			if (rolledAmount == 6) {
				atBase = false;
				TargetMoveToStart(connectionToClient);
			}
		} else {
			while (rolledAmount > 0) {
				rolledAmount--;

				if ((currentTileIndex + 1) == gci.numberOfTiles) {
					currentTileIndex = 0;
				} else if (currentTileIndex == lastTileIndex) {
					inner = true;
					currentTileIndex = 0;
					rolledAmount++;
					yield return false;
				} else if (inner) {
					Debug.Log("currentTileIndex: " + currentTileIndex + ", rolledAmount: " + rolledAmount + ", total: " + (currentTileIndex + rolledAmount));
					if ((currentTileIndex + rolledAmount) > 5) {
						break;
					} else if (currentTileIndex != 5 && rolledAmount > 0) { // prevents 5 + 0, OOB Exception
						currentTileIndex++;
					//} else { // Should not happen
					//	break;
					}
				} else {
					currentTileIndex++;
				}

				atFinish = gci.path[currentTileIndex].tag == "Finish";
				TargetMove(connectionToClient, currentTileIndex, inner);

				yield return new WaitForSeconds(0.0625f);
			}
		}
	}

	[TargetRpc]
	private void TargetMove(NetworkConnection connection, int location, bool inner) {
		Vector3 position;

		if (inner) {
			position = gci.player[slot].innerPath[location].position;
		} else {
			position = gci.path[location].transform.position;
		}

		transform.position = position;
	}

	[TargetRpc]
	public void TargetInit(NetworkConnection connection, int slot) {
		if (slot != -1) {
			this.slot = slot;
		}
	}

	public override void OnStartClient() {
		EnableRendering(render);
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
		GetComponent<MeshRenderer>().enabled = status;
	}

	[TargetRpc]
	public void TargetMoveToBase(NetworkConnection connection) {
		for (int i = 0; i < gci.bases[slot].transform.childCount; ++i) {
			Transform childTransform = gci.bases[slot].transform.GetChild(i);

			BaseSlotController bsc = childTransform.GetComponent<BaseSlotController>();
			if (!bsc.occupied) {
				transform.position = childTransform.position;

				//EnableRendering(true);

				if (hasAuthority) {
					CmdEnableRendering(true);
				}

				atBase = true;
				bsc.occupied = true;
				currentTileIndex = i;
				break;
			}
		}
	}

	[TargetRpc]
	private void TargetMoveToStart(NetworkConnection connection) {
		gci.bases[slot].transform.GetChild(currentTileIndex).GetComponent<BaseSlotController>().occupied = false;
		transform.position = gci.path[startTileIndex].transform.position;
	}

}
