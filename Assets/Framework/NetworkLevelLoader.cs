using UnityEngine;
using System.Collections;

public class NetworkLevelLoader : NetScript {
	void AllLoadLevel (int level) {
		if (!backstab.IsServer) {
			Debug.LogError("Can't issue LoadLevel messages if not the server.");
			return;
		}
		Application.LoadLevel(level);
		Rpc("LoadLevel", level);
	}

	[RpcClients]
	void LoadLevel (int level) {
		Application.LoadLevel(level);
	}
}
