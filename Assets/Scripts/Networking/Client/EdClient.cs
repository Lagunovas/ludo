using UnityEngine;
using Mirror;

public class EdClient : MonoBehaviour {

	public static EdClient Instance {
		get;
		private set;
	}

	private readonly NetworkClient networkClient;

	private void Awake() {
		if (Instance != null && Instance != this) {
			Destroy(gameObject);
		} else {
			Instance = this;
		}
	}

	private void Start() {
		GetComponent<EdNetworkManager>().customOnStartClient = OnStartClient;
	}

	private void OnStartClient() {
		RegisterPrefabs();
		RegisterClientMessages();
	}

	private void RegisterPrefabs() {
		// Make ASYNC?
		foreach (GameObject prefab in Resources.LoadAll<GameObject>("Prefabs")) {
			if (prefab.GetComponent<NetworkIdentity>()) {
				ClientScene.RegisterPrefab(prefab);
			}
		}
	}

	public void RegisterClientMessages() {
		NetworkClient.RegisterHandler<ConnectMessage>(OnClientConnect);
		NetworkClient.RegisterHandler<DisconnectMessage>(OnClientDisconnect);
		NetworkClient.RegisterHandler<ReadyMessage>(AddPlayer);
	}

	private void OnClientConnect(NetworkConnection connection, ConnectMessage message) {
		Debug.Log("Connected");

		gameObject.GetComponent<NetworkManagerHUD>().showGUI = false;

		// before add player is done, get a packet from the server allowing to connect. (if not awaiting_for_players state then kick)
		ClientScene.Ready(connection);
	}

	private void OnClientDisconnect(NetworkConnection connection, DisconnectMessage message) {
		Debug.Log("Disconnected");
	}

	#region Messages that will be sent to the server

	//public void AAA(Item item) {
	//	NetworkClient.Send(new CraftingRequest {
	//		itemHashcode = item.GetHashCode()
	//	});
	//}

	#endregion

	#region Messages received from server

	private void AddPlayer(NetworkConnection connection, ReadyMessage message) {
		UIDMessage uidMessage = new UIDMessage {
			//uid = AuthenticationController.Instance.UID
			uid = "testUID"
		};

		ClientScene.AddPlayer(connection, MessagePacker.Pack(uidMessage));
	}

	#endregion

	#region MISC/Helper methods

	public bool IsConnected() => NetworkClient.connection != null && NetworkClient.isConnected;

	#endregion
}