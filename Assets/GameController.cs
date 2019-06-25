using System.Collections.Generic;
using UnityEngine;

using Mirror;

public class GameController : NetworkBehaviour {

	private enum GameState {
		AWAITING_FOR_PLAYERS,
		STARTED

	}

	public class Player {
		public int startTileIndex;
		public Transform[] innerPath;
		public int lastTileIndex;

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

	private bool[] slots = null;

	[SyncVar] public int currentTurn = -2;

	public GameObject pawnPrefab = null;

	private GameState state = GameState.AWAITING_FOR_PLAYERS;

	public static GameController Instance {
		get;
		private set;
	}

	private void Awake() {
		if (Instance != null && Instance != this) {
			Destroy(gameObject);
		} else {
			Instance = this;

			currentTurn = 0;
			bases = new List<GameObject>();
			player = new List<Player>();
			path = new List<GameObject>();

			ProcessMap();

			slots = new bool[player.Count];

			Random.InitState((int)System.DateTime.Now.Ticks);
		}
	}

	public int RollDice() => Random.Range(1, 7);

	public int AssignPlayerSlot() {
		for (int i = 0; i < slots.Length; ++i) {
			if (!slots[i]) {
				slots[i] = true;
				return i;
			}
		}

		return -1;
	}

	private bool AllSlotsFilled() {
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

			}
		}
	}

	private void ProcessMap() {
		int lastTileIndex = -1;

		foreach (Transform childTransform in transform) {
			GameObject go = childTransform.gameObject;
			if (go.activeInHierarchy) { // ignore center tile (or use ignore tag)
				switch (childTransform.tag) {
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
					case "Safe":
						path.Add(go);
						break;
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
