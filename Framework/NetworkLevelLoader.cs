using UnityEngine;

public class NetworkLevelLoader : NetScript {
	static int lastLevel = -108;

	bool[] status;

	public void AllLoadLevel (int level) {
		if (!backstab.IsServer) {
			Debug.LogError("Can't issue LoadLevel messages if not the server.");
			return;
		}
		DontDestroyOnLoad(this);
		status = new bool[backstab.NumConnections+1];
		Rpc("LoadLevel", level);
	}

	[RpcAll]
	private void LoadLevel (int level) {
		Application.LoadLevel(level);
		if (backstab.IsClient) {
			Rpc("OnClientFinishedLoading");
		}
		lastLevel = level;
	}

	[RpcServer]
	public void OnClientFinishedLoading () {
		status[backstab.recConnectionId] = true;

		foreach (bool b in status) {
			if (!b) return;
		}

		Rpc("OnLoadingFinished", lastLevel);
	}

	[RpcAll]
	public void OnLoadingFinished (int level) {
		foreach (NetScript n in NetScript.Instances) {
			n.OnNetworkLoadedLevel(level);
		}
	}
}
