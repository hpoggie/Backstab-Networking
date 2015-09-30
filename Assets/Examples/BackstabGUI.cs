﻿using UnityEngine;

public class BackstabGUI : NetScript {
	private ConnectionData serverData;

	void OnGUI () {
		if (backstab != null) {
			GUILayout.Box("Your socket ID is " +backstab.LocalSocketId);
			if (backstab.IsServer) {
				GUILayout.Box("Server active.");
			} else if (backstab.IsClient) {
				GUILayout.Box("Client active.");
			} else if (backstab.IsClient && backstab.IsConnected) {
				GUILayout.Box("Connected to " + serverData.address);
			} else {
				GUILayout.Box("Not server or client.");
			}
			foreach (ConnectionData b in backstab.broadcasters) {
				if (GUILayout.Button(b.ToString())) {
					backstab.Connect(b.address, b.port);
				}
			}
		}
	}

	public override void OnBackstabConnectedToServer (ConnectionData data) {
		serverData = data;
	}
}
