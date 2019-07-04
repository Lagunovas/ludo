using UnityEngine;

using Mirror;
using System.Collections.Generic;
using System.Collections;

using UnityEngine.UI;

public class PlayerController : NetworkBehaviour {

	[SyncVar(hook = nameof(OnChangeSlot))] public int slot = -1; // For update, pawn controller has to use seperate
	public bool pawnSelected = false;

	private void OnChangeSlot(int slot) {
		this.slot = slot;

		if (slot != -1) {
			GameController.Instance.player[slot].diceImage.gameObject.GetComponent<Button>().onClick.AddListener(() => ClientRoll());
			//GameController.Instance.ChangeDiceSide(slot, -1);
		}
	}

	[Client]
	private void ClientRoll() {
		if (isClient) {
			if (hasAuthority) {
				if (slot == GameController.Instance.currentTurn) {
					if (rollsLeft > 0) {
						if (rolledAmount == 0) {
							CmdRoll();
						}
					}
				}
			}
		}
	}
	// SERVER ONLY

	public List<PawnController> pawns;
	[SyncVar(hook = nameof(OnChangeRollsLeft))] public int rollsLeft; // display on client
	[SyncVar] public int rolledAmount;
	private int sixInARow;

	void OnChangeRollsLeft(int rollsLeft) {
		this.rollsLeft = rollsLeft;

		if (slot != -1) {
			GameController.Instance.player[slot].rollsLeftTMP.text = "Rolls Left: " + rollsLeft;
		}
	}

	[Server]
	public bool IsTurnFinished() => rollsLeft == rolledAmount && rolledAmount == 0;

	//

	private void Awake() {
		pawns = new List<PawnController>();
	}

	private void Start() {
		
	}

	private void Update() {
		if (isClient) {
			if (hasAuthority) {
				if (slot == GameController.Instance.currentTurn) {
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

	[SyncVar(hook = nameof(OnChangeDiceSide))] public int diceSide = -1;

	private void OnChangeDiceSide(int diceSide) {
		this.diceSide = diceSide;
		GameController.Instance.ChangeDiceSide(slot, diceSide);
	}

	[Server]
	private int RandomUnique() {
		int randomUniqueNumber;

		do {
			randomUniqueNumber = Random.Range(0, 6);
		} while (randomUniqueNumber == previousRandomNumber);

		return previousRandomNumber = randomUniqueNumber;
	}

	private int previousRandomNumber = -1;
	private bool rolling;

	[Command]
	private void CmdRoll() {
		ServerRoll();
	}

	[Server]
	private void ServerRoll() {
		if (rolling == false) {
			rolling = true;
			StartCoroutine(Roll(Random.Range(5, 15)));
		}
	}

	private IEnumerator Roll(int times) {
		if (rollsLeft > 0) {
			if (rolledAmount == 0) {

				while (times > 0) {
					times--;
					diceSide = RandomUnique();
					yield return new WaitForSeconds(0.075f);
				}

				int rollsLeft = this.rollsLeft - 1;
				int rolledAmount = GameController.Instance.RollDice();

				diceSide = previousRandomNumber = rolledAmount - 1;

				yield return new WaitForSeconds(0.075f);

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
					AttemptAutoMove(rollsLeft, rolledAmount);
				}

				Debug.Log("rollsLeft: " + rollsLeft);
				Debug.Log("rolledAmount: " + rolledAmount);

				rolling = false;
			}
		}
	}

	public void ResetSixCount() {
		sixInARow = 0;
	}

	[Server]
	private void AttemptAutoMove(int rollsLeft, int rolledAmount) {
		int possibleMoves = 0;
		int staged = 0;
		PawnController stagedPawn = null;
		int index = -1;

		for (int i = 0; i < pawns.Count; ++i) {
			PawnController pawn = pawns[i];
			if (pawn.SetCanSelect(rolledAmount)) {
				possibleMoves++;

				if (pawn.IsStaged) {
					staged++;
					stagedPawn = pawn;
				}

				index = i;
			}
		}

		if (possibleMoves == staged && possibleMoves != 0) { // TEST
			possibleMoves = 1;
		}

		switch (possibleMoves) {
			case 0:
				this.rollsLeft = this.rolledAmount = 0;
				break;
			case 1:
				PawnController pawn = (staged > 0) ? stagedPawn : pawns[index];
				this.rolledAmount = rolledAmount;
				this.rollsLeft = rollsLeft;
				pawn.isSelected = true;
				// Must be done here, otherwise in loop will skip players turn
				pawn.ServerMove(rolledAmount);
				break;
			default:
				this.rollsLeft = rollsLeft;
				this.rolledAmount = rolledAmount;
				break;
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

			GameController.Instance.player[slot].diceImage.gameObject.GetComponent<Button>().onClick.AddListener(() => ServerRoll());

			foreach (PawnController pawn in pawns) {
				pawn.Init(this);
				pawn.TargetMoveToBase(connectionToClient);
			}
		}
	}

}
