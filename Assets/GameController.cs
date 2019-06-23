//using System;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour {

	public class TESTCLASS {
		public int startTileIndex;
		public Transform[] innerPath;
		public int lastTileIndex;

		public TESTCLASS(int startTileIndex, int lastTileIndex) {
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

	private List<GameObject> bases = null;
	// tile id, starting parent object, last tile
	[HideInInspector] public List<TESTCLASS> slotLocations = null;
	[HideInInspector] public List<GameObject> path = null;

	public int numberOfTiles;

	private bool[] slots = null;

	public static GameController Instance {
		get;
		private set;
	}

	private void Awake() {
		if (Instance != null && Instance != this) {
			Destroy(gameObject);
		} else {
			Instance = this;
		}
	}

	public int RollDice() => Random.Range(1, 7);

	public (int, TESTCLASS) AssignPlayer() {
		for (int i = 0; i < slots.Length; ++i) {
			if (!slots[i]) {
				slots[i] = true;
				return (i, slotLocations[i]);
			}
		}

		return (-1, null);
	}

	private void Start() {
		bases = new List<GameObject>();
		slotLocations = new List<TESTCLASS>();
		path = new List<GameObject>();

		slots = new bool[4]; // ????

		ProcessMap();

		Random.InitState((int) System.DateTime.Now.Ticks);
	}

	private void Update() {

	}

	private void ProcessMap() {
		int lastTileIndex = -1;

		foreach (Transform transform in transform) {
			GameObject go = transform.gameObject;
			if (go.activeInHierarchy) { // ignore center tile (or use ignore tag)
				switch (transform.tag) {
					case "Base":
						bases.Add(go);
						break;
					case "Start":
						path.Add(go);
						slotLocations.Add(new TESTCLASS(path.Count - 1, lastTileIndex));
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

		TESTCLASS a = slotLocations[0];
		a.lastTileIndex = lastTileIndex;

		numberOfTiles = path.Count;
	}

}
