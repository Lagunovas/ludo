using System.Collections.Generic;
using UnityEngine;

using TMPro;

using Mirror;

public class GameController : NetworkBehaviour {

	public enum GameState {
		AWAITING_FOR_PLAYERS,
		STARTED

	}

	private TextMeshProUGUI currentTurnTMP = null;

	public class Player {
		public int startTileIndex;
		public Transform[] innerPath;
		public int lastTileIndex;

		public TextMeshProUGUI rollsLeftTMP;
		public TextMeshProUGUI rolledAmountTMP;

		public Player(int startTileIndex, int lastTileIndex) {
			this.startTileIndex = startTileIndex;

			GameObject startTile = Instance.path[startTileIndex];
			int childCount = startTile.transform.childCount;

			innerPath = new Transform[childCount];

			for (int i = 0; i < childCount; ++i) {
				innerPath[i] = startTile.transform.GetChild(i);
			}

			this.lastTileIndex = lastTileIndex;
		}
	}

	[HideInInspector] public List<GameObject> bases = null;
	[HideInInspector] public List<Player> player = null;
	[HideInInspector] public List<GameObject> path = null;

	public int numberOfTiles;

	public PlayerController[] slots = null;

	[SyncVar(hook = "ChangeCurrentTurn")] public int currentTurn = -2;

	public void ChangeCurrentTurn(int newCurrentTurn) {
		currentTurn = newCurrentTurn;
		currentTurnTMP.text = "Current Turn: " + (currentTurn + 1);
	}

	public GameObject pawnPrefab = null;

	public GameState state = GameState.AWAITING_FOR_PLAYERS;

	public Material[] materials = null;

	// AUDIO

	public AudioClip[] sounds = null;

	public static GameController Instance {
		get;
		private set;
	}

	private void Awake() {
		if (Instance != null && Instance != this) {
			Destroy(gameObject);
		} else {
			Instance = this;

			bases = new List<GameObject>();
			player = new List<Player>();
			path = new List<GameObject>();

			ProcessMap();

			slots = new PlayerController[bases.Count];

			Random.InitState((int) System.DateTime.Now.Ticks);
		}
	}

	public int RollDice() => Random.Range(1, 7);

	public int AssignPlayerSlot(PlayerController playerController) {
		for (int i = 0; i < slots.Length; ++i) {
			if (slots[i] == null) {
				slots[i] = playerController;
				return i;
			}
		}

		return -1;
	}

	[Server]
	public bool AllSlotsFilled() {
		foreach (bool filled in slots) {
			if (!filled) {
				return false;
			}
		}

		state = GameState.STARTED;
		return true;
	}

	private void FixedUpdate() {
		if (isServer) {
			if (state == GameState.STARTED || AllSlotsFilled()) {
				if (currentTurn == -2) {
					currentTurn = 0;
					slots[currentTurn].rollsLeft = 1;
				} else {
					if (slots[currentTurn]) { // skip player if left
						if (slots[currentTurn].IsTurnFinished()) {
							slots[currentTurn++].ResetSixCount();
						}
					} else {
						currentTurn++;
					}

					if (currentTurn == bases.Count) {
						currentTurn = 0;
					}

					slots[currentTurn].rollsLeft = 1;
				}
			}
		}
	}

	private void ProcessMap() {
		int lastTileIndex = -1;

		foreach (Transform childTransform in transform) {
			GameObject go = childTransform.gameObject;
			if (go.activeInHierarchy) { // ignore center tile (or use ignore tag)
				switch (childTransform.tag) {
					case "CurrentTurn":
						currentTurnTMP = childTransform.GetChild(0).GetComponent<TextMeshProUGUI>();
						break;
					case "UI":
						Player currentPlayer = player[player.Count - 1];
						currentPlayer.rollsLeftTMP = childTransform.GetChild(0).GetComponent<TextMeshProUGUI>();
						currentPlayer.rolledAmountTMP = childTransform.GetChild(1).GetComponent<TextMeshProUGUI>();
						break;
					case "Base":
						bases.Add(go);
						break;
					case "Start":
						path.Add(go);
						player.Add(new Player(path.Count - 1, lastTileIndex));
						lastTileIndex = -1;
						break;
					case "Finish":
						break;
					case "Last":
						lastTileIndex = path.Count;
						path.Add(go);
						break;
					case "Ignore":
						continue;
					//case "Safe":
					//	path.Add(go);
					//	break;
					default:
						path.Add(go);
						break;
				}
			}
		}

		Player a = player[0];
		a.lastTileIndex = lastTileIndex;

		numberOfTiles = path.Count;
	}

}
