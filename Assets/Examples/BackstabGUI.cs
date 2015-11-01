using UnityEngine;

public class BackstabGUI : NetScript {
	private ConnectionData serverData;

	public string ipAddress;

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
		}
	}

	public override void OnBackstabConnectedToServer (ConnectionData data) {
		serverData = data;
	}
}
