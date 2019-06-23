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

		(int, GameController.TESTCLASS) returnValue = GameController.Instance.AssignPlayer();

		if (returnValue.Item1 >= 0) {
			PlayerController pc = connection.playerController.gameObject.GetComponent<PlayerController>();
			pc.AssignSlot(returnValue.Item1, returnValue.Item2.startTileIndex, returnValue.Item2.lastTileIndex);
		} else {
			connection.Disconnect();
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

	// Destroyed in the base call from FRNetworkManager
	private void OnDisconnect(NetworkMessage message) {
		Debug.Log("client disconnected.");

		//FRDatabase.FRPlayers.Remove(message.conn.connectionId);
	}

}
