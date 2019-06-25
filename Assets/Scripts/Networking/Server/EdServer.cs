using UnityEngine;

using Mirror;

public sealed class EdServer : MonoBehaviour {

	public static EdServer Instance {
		get;
		private set;
	}

	private void Awake() {
		if (Instance != null && Instance != this) {
			Destroy(gameObject);
		} else {
			Instance = this;
			//EdDatabase.Init();
		}
	}

	private void Start() {
		EdNetworkManager edNetworkManager = GetComponent<EdNetworkManager>();
		edNetworkManager.registerCustomHandlers = RegisterHandlers;
		edNetworkManager.customOnServerConnect = OnConnect;
		edNetworkManager.customOnServerReady = OnPlayerReady;
		edNetworkManager.customOnServerAddPlayer = OnServerAddPlayer;
	}

	private void OnServerAddPlayer(NetworkConnection connection, AddPlayerMessage extraMessage, System.Action baseMethodCallback) { // called when client call add player
																																	//UIDMessage message = MessagePacker.Unpack<UIDMessage>(extraMessage.value);

		//Debug.Log(message.uid);

		//EdDatabase.GetPlayerData(connection.connectionId, message.uid, match => {
		//	if (match) {
		//		baseMethodCallback.Invoke();
		//	} else {
		//		connection.Disconnect();
		//	}
		//}

		baseMethodCallback.Invoke();

		PlayerController playerController = connection.playerController.gameObject.GetComponent<PlayerController>();

		if (playerController) {
			int assignedSlot = GameController.Instance.AssignPlayerSlot();

			if (assignedSlot >= 0) {
				for (uint i = 0; i < GameController.Instance.bases.Count; ++i) {
					GameObject newPawn = Instantiate(GameController.Instance.pawnPrefab);
					NetworkServer.SpawnWithClientAuthority(newPawn, connection);
					playerController.pawns.Add(newPawn.GetComponent<PawnController>());
				}

				playerController.AssignSlot(assignedSlot);
			} else {
				connection.Disconnect();
			}
		}
	}

	private void OnPlayerReady(NetworkConnection connection) {
		//if (GameManager.Instance) {
		//	if (GameManager.Instance.state == GameManager.GameState.AWAITING_FOR_PLAYERS) {
		//		NetworkServer.SendToClient(connection.connectionId, new ReadyMessage());
		//	} else {
		//		connection.Disconnect();
		//		//connection.Dispose();
		//	}
		//} else {
		//	NetworkServer.SendToClient(connection.connectionId, new ReadyMessage());
		//}

		NetworkServer.SendToClient(connection.connectionId, new ReadyMessage());
	}

	private void RegisterHandlers() {
		//NetworkServer.RegisterHandler(MsgType.AddPlayer, OnAddPlayer);
		//NetworkServer.RegisterHandler(MsgType.RemovePlayer, OnServerRemovePlayerMessageInternal);
		//NetworkServer.RegisterHandler(MsgType.Error, OnServerErrorInternal);

		//if (GameManager.Instance) {
		//	FRDatabase.GetServerAddKey((serverAddKey) => {
		//		if (!string.IsNullOrEmpty(serverAddKey)) {
		//			StartCoroutine(GameManager.Instance.ListServer(serverAddKey));
		//		}
		//	});
		//}
	}

	private void OnConnect(NetworkConnection connection) {
		Debug.Log("Client Connected.");
	}

	// Destroyed in the base call from EdNetworkManager
	private void OnDisconnect(NetworkMessage message) {
		Debug.Log("client disconnected.");

		//EdDatabase.FRPlayers.Remove(message.conn.connectionId);
	}

}
