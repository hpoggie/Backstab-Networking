using UnityEngine;

public class BackstabGUI : NetScript {
	private ConnectionData serverData;

	public string ipAddress;

	public int nextLevel;
	public NetworkLevelLoader levelLoader;

	void OnGUI () {
		if (backstab != null) {
			GUILayout.Box("Your socket ID is " +backstab.LocalSocketId);
			if (backstab.IsServer) {
				GUILayout.Box("Server active.");
			} else if (backstab.IsClient && backstab.IsConnected) {
				GUILayout.Box("Connected to " + serverData.address);
			} else if (backstab.IsClient) {
				GUILayout.Box("Client active.");
			} else {
				if (GUILayout.Button("Start Server")) backstab.StartServer();
				if (GUILayout.Button("Start Client")) backstab.StartClient();
				GUILayout.Box("Not server or client.");
			}
			
			foreach (ConnectionData b in backstab.broadcasters) {
				if (GUILayout.Button(b.ToString())) {
					backstab.Connect(b.address);
				}
			}
			
			ipAddress = GUILayout.TextField(ipAddress);

			if (GUILayout.Button("Connect")) {
				backstab.Connect(ipAddress);
			}

			if (GUILayout.Button("Load Level")) levelLoader.AllLoadLevel(nextLevel);
		}
	}

	public override void OnBackstabConnectedToServer (ConnectionData data) {
		serverData = data;
	}
}
