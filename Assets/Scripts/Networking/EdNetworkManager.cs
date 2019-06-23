using Mirror;

public class EdNetworkManager : NetworkManager {

	public string masterServerIp = null;
	public int masterServerPort = 8443;

	public string loginServerIp = null;
	public int loginServerPort = 8444;

	#region Server delegates
	public delegate void RegisterCustomHandlers();
	public RegisterCustomHandlers registerCustomHandlers;

	public delegate void CustomOnServerConnect(NetworkConnection conenction);
	public CustomOnServerConnect customOnServerConnect;

	public delegate void CustomOnServerDisconnect(NetworkConnection connection);
	public CustomOnServerDisconnect customOnServerDisconnect;

	public delegate void CustomOnServerReady(NetworkConnection connection);
	public CustomOnServerReady customOnServerReady;

	public delegate void CustomOnServerAddPlayer(NetworkConnection connection, AddPlayerMessage extraMessage, System.Action baseMethodCallback);
	public CustomOnServerAddPlayer customOnServerAddPlayer;
	#endregion

	#region Client delegates
	public delegate void CustomOnStartClient();
	public CustomOnStartClient customOnStartClient;
	#endregion

	//public override void Awake() {
	//	base.Awake();
	//}

	//public override void LateUpdate() {
	//	base.LateUpdate();
	//}

	//public override void OnApplicationQuit() {
	//	base.OnApplicationQuit();
	//}

	//public override void OnClientConnect(NetworkConnection conn) {
	//	base.OnClientConnect(conn);
	//}

	//public override void OnClientDisconnect(NetworkConnection conn) {
	//	base.OnClientDisconnect(conn);
	//}

	//public override void OnClientError(NetworkConnection conn, int errorCode) {
	//	base.OnClientError(conn, errorCode);
	//}

	//public override void OnClientNotReady(NetworkConnection conn) {
	//	base.OnClientNotReady(conn);
	//}

	//public override void OnClientSceneChanged(NetworkConnection conn) {
	//	base.OnClientSceneChanged(conn);
	//}

	//public override void OnDestroy() {
	//	base.OnDestroy();
	//}

	public override void OnServerAddPlayer(NetworkConnection connection, AddPlayerMessage extraMessage) {
		customOnServerAddPlayer?.Invoke(connection, extraMessage, () => base.OnServerAddPlayer(connection, extraMessage));
	}

	//public override void OnServerAddPlayer(NetworkConnection conn) {
	//	base.OnServerAddPlayer(conn);
	//}

	public override void OnServerConnect(NetworkConnection connection) {
		customOnServerConnect?.Invoke(connection);
	}

	public override void OnServerDisconnect(NetworkConnection connection) {
		customOnServerDisconnect?.Invoke(connection);
		base.OnServerDisconnect(connection);
	}

	//public override void OnServerError(NetworkConnection conn, int errorCode) {
	//	base.OnServerError(conn, errorCode);
	//}

	//public override void OnServerReady(NetworkConnection conn) {
	//	base.OnServerReady(conn);
	//}

	public override void OnServerReady(NetworkConnection connection) {
		base.OnServerReady(connection);
		customOnServerReady?.Invoke(connection);
	}

	//public override void OnServerRemovePlayer(NetworkConnection conn, NetworkIdentity player) {
	//	base.OnServerRemovePlayer(conn, player);
	//}

	//public override void OnServerSceneChanged(string sceneName) {
	//	base.OnServerSceneChanged(sceneName);
	//}

	public override void OnStartClient() {
		customOnStartClient();
	}

	//public override void OnStartHost() {
	//	base.OnStartHost();
	//}

	public override void OnStartServer() {
		registerCustomHandlers?.Invoke();
	}

	//public override void OnStopClient() {
	//	base.OnStopClient();
	//}

	//public override void OnStopHost() {
	//	base.OnStopHost();
	//}

	//public override void OnStopServer() {
	//	base.OnStopServer();
	//}

	//public override void OnValidate() {
	//	base.OnValidate();
	//}

	//public override void ServerChangeScene(string newSceneName) {
	//	base.ServerChangeScene(newSceneName);
	//}

	//public override NetworkClient StartHost() {
	//	return base.StartHost();
	//}

}
