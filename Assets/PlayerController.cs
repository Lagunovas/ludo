using UnityEngine;

using Mirror;
using System.Collections.Generic;

public class PlayerController : NetworkBehaviour {

	[SyncVar] public int slot = -1; // For update, pawn controller has to use seperate
	public bool pawnSelected = false;

	// SERVER ONLY

	public List<PawnController> pawns;
	[SyncVar(hook = nameof(OnChangeRollsLeft))] public int rollsLeft; // display on client
	[SyncVar(hook = nameof(OnChangeRolledAmount))] public int rolledAmount;
	private int sixInARow;

	void OnChangeRollsLeft(int rollsLeft) {
		this.rollsLeft = rollsLeft;

		if (slot != -1) {
			GameController.Instance.player[slot].rollsLeftTMP.text = "Rolls Left: " + rollsLeft;
		}
	}

	void OnChangeRolledAmount(int rolledAmount) {
		this.rolledAmount = rolledAmount;

		if (slot != -1) {
			GameController.Instance.player[slot].rolledAmountTMP.text = "Rolled: " + rolledAmount;
		}
	}

	[Server]
	public bool IsTurnFinished() => rollsLeft == rolledAmount && rolledAmount == 0;

	//

	private void Awake() {
		pawns = new List<PawnController>();
	}

	private void Update() {
		if (isClient) {
			if (hasAuthority) {
				if (slot == GameController.Instance.currentTurn) {
					if (rollsLeft > 0) {
						if (rolledAmount == 0) {
							if (Input.GetKeyDown(KeyCode.Space)) {
								CmdRoll();
							}
						}
					}

					if (Input.GetKeyDown(KeyCode.R)) {
						CmdSkipTurn();
					}

					if (!pawnSelected && rolledAmount > 0) {
						if (Input.GetMouseButtonDown(0)) {
							Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

							RaycastHit[] hits = Physics.RaycastAll(ray);

							foreach (RaycastHit hit in hits) {
								GameObject go = hit.collider.gameObject;
								if (go.tag == "Pawn") {
									Transform gopt = go.transform.parent;
									if (gopt.childCount == 2) {
										PawnController pawnController = gopt.gameObject.GetComponent<PawnController>();
										if (pawnController.playerController) { // wont be initialised for players who joined after, no issue
											if (slot != -1 && slot == pawnController.playerController.slot) {
												if (pawnController.IsSelectable()) {
													CmdOnSelectPawn(gopt.GetComponent<NetworkIdentity>().netId);
													break;
												}
											}
										}
									}
								}
							}

						}
					}

				}
			}
		} else { // server
			//foreach (PawnController pawn in pawns) {
			//	pawn.SetCanSelect(!pawnSelected);
			//}
		}
	}

	[Command]
	private void CmdSkipTurn() {
		rollsLeft = rolledAmount = 0;
	}

	[Command]
	private void CmdRoll() {
		if (rollsLeft > 0) {
			if (rolledAmount == 0) {
				int rollsLeft = this.rollsLeft - 1;
				int rolledAmount = GameController.Instance.RollDice();

				if (rolledAmount == 6) {
					rollsLeft++;
					sixInARow++;
				} else {
					sixInARow = 0;
				}

				if (sixInARow == 3) {
					this.rollsLeft = this.rolledAmount = 0; // sixInArow is reset in the GameController.
					Debug.Log("Rolled 6 three times in a row, skip turn.");
				} else {
					if (AbleToMove(rolledAmount)) {
						this.rollsLeft = rollsLeft;
						this.rolledAmount = rolledAmount;
					} else {
						Debug.Log("Unable to move, skip turn!");
						this.rollsLeft = 0;
						this.rolledAmount = 0;
					}

					//this.rollsLeft = rollsLeft;
				}

				Debug.Log("rollsLeft: " + rollsLeft);
				Debug.Log("rolledAmount: " + rolledAmount);
			}
		}
	}

	public void ResetSixCount() {
		sixInARow = 0;
	}

	[Server]
	private bool AbleToMove(int rolledAmount) {
		int possibleMoves = 0;
		int index = -1;

		for (int i = 0; i < pawns.Count; ++i) {
			PawnController pawn = pawns[i];
			if (pawn.SetCanSelect(rolledAmount)) {
				possibleMoves++;
				index = i;
			}
		}

		switch (possibleMoves) {
			case 0:
				return false;
			case 1:
				// move automatically
				PawnController pawn = pawns[index];
				// move this pawn
				// set selected
				return true;
			default:
				return true;
		}
	}

	[Command]
	private void CmdOnSelectPawn(uint netId) {
		Debug.Log("select called");
		if (NetworkIdentity.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)) {
			pawnSelected = true;
			//TargetOnSelectPawn(connectionToClient);
			networkIdentity.gameObject.GetComponent<PawnController>().OnSelect();
		}
	}


	[Server]
	public void AssignSlot(int slot) {
		if (this.slot == -1) {
			this.slot = slot;

			foreach (PawnController pawn in pawns) {
				pawn.Init(this);
				pawn.TargetMoveToBase(connectionToClient);
			}
		}
	}

}
