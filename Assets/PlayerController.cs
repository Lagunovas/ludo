using UnityEngine;
using Mirror;
using System.Collections;

public class PlayerController : NetworkBehaviour {

	private int location; // order index
	private int assignedSlot = -1;
	private int startTileIndex = -1;
	private int lastTileIndex = -1;
	private bool inner;
	private bool inBase;
	private void Start() {

	}

	// MOVE ELSEWHERE, CHECK IF SELECTED AFTER DIE WAS ROLLED
	// Do not allow to select if inner && location == 5
	private void Update() {
		if (isLocalPlayer) {
			//if (Input.GetKeyDown(KeyCode.Space)) {
			//	CmdMove();
			//}
		}
	}

	public void AssignSlot(int assignedSlot, int startTileIndex, int lastTileIndex) {
		if (this.assignedSlot == -1) {
			this.assignedSlot = assignedSlot;
			this.startTileIndex = location = startTileIndex;
			this.lastTileIndex = lastTileIndex;
			RpcMoveToBase(assignedSlot);
		}
	}

	[Command]
	private void CmdMove() {
		StartCoroutine(Move(GameController.Instance.RollDice()));
	}

	[Server]
	private IEnumerator Move(int rolledAmount) {
		//GameObject currentTile = GameController.Instance.order[location];

		while (rolledAmount > 0) {
			rolledAmount--;

			if ((location + 1) == GameController.Instance.numberOfTiles) {
				RpcMove(location = 0, false);
			} else if (location == lastTileIndex) {
				inner = true;
				location = 0;
				rolledAmount++;
				yield return false;
			} else if (inner) {
				if ((location + rolledAmount) > 5) {
					break;
				} else {
					RpcMove(location++, true);
				}
			} else {
				int nextLocation = location + 1;
				RpcMove(location = nextLocation, false);
			}

			yield return new WaitForSeconds(0.0625f);
		}

	}

	[ClientRpc]
	private void RpcMove(int location, bool inner) {
		Vector3 position;

		if (inner) {
			position = GameController.Instance.slotLocations[assignedSlot].innerPath[location].position;
		} else {
			position = GameController.Instance.path[location].transform.position;
		}

		transform.position = position;
	}

	[ClientRpc]
	private void RpcMoveToBase(int index) {
		foreach (Transform childTransform in GameController.Instance.bases[index].transform) {
			if (childTransform.childCount == 0) {
				transform.position = childTransform.position;
				transform.parent = childTransform;
				MeshRenderer mr = GetComponent<MeshRenderer>();
				//mr.material = ....
				mr.enabled = true;
				inBase = true;
				return;
			}
		}
	}

	[ClientRpc]
	private void RpcMoveToStart() {
		transform.position = GameController.Instance.path[startTileIndex].transform.position;
	}



}
