using UnityEngine;

using Mirror;
using System.Collections.Generic;

public class PlayerController : NetworkBehaviour {

	[SyncVar] public int slot = -1;
	//private bool pawnSelected;

	// SERVER ONLY

	public List<PawnController> pawns;
	private int rolledAmount;

	//

	private void Awake() {
		pawns = new List<PawnController>();
	}

	private void Update() {
		if (isLocalPlayer) {
			if (slot == GameController.Instance.currentTurn) {
				if (isClient) {
					if (Input.GetMouseButtonDown(0)) {
						Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

						if (Physics.Raycast(ray, out RaycastHit hit)) {
							GameObject go = hit.collider.gameObject;
							if (go.tag == "Pawn") {
								CmdOnSelectPawn(go.GetComponent<NetworkIdentity>().netId);
							}
						}
					}
				}
			}
		}
	}

	[Command]
	private void CmdOnSelectPawn(uint netId) {
		if (NetworkIdentity.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)) {
			networkIdentity.gameObject.GetComponent<PawnController>().OnSelect();
			RpcOnSelectPawn();
		}
	}


	private void RpcOnSelectPawn() {
		//pawnSelected = true;
	}

	public void AssignSlot(int slot) {
		if (this.slot == -1) {
			this.slot = slot;
			//GameController.Player player = GameController.Instance.player[slot];
			//startTileIndex = currentTileIndex = player.startTileIndex;
			//lastTileIndex = player.lastTileIndex;
			//RpcMoveToBase(slot);

			foreach (PawnController pawn in pawns) {
				pawn.TargetInit(connectionToClient, slot);
				pawn.TargetMoveToBase(connectionToClient);
			}
		}
	}

}
