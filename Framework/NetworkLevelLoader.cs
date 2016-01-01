using UnityEngine;
using UnityEngine.Networking;

public class NetworkLevelLoader : NetScript {
	static int lastLevel = -108;

	bool[] status;

	public void Start () {
		DontDestroyOnLoad(this);
	}

	public void AllLoadLevel (int level) {
		if (!backstab.IsServer) {
			Debug.LogError("Can't issue LoadLevel messages if not the server.");
			return;
		}
		status = new bool[backstab.NumConnections+1];
		Rpc("LoadLevel", level);
	}

	[RpcAll(qosType = QosType.ReliableSequenced)]
	private void LoadLevel (int level) {
		Application.LoadLevel(level);
		if (backstab.IsClient) {
			Rpc("OnClientFinishedLoading");
		} else {
			backstab.recConnectionId = 0;
			OnClientFinishedLoading();
		}
		lastLevel = level;
	}

	[RpcServer(qosType = QosType.ReliableSequenced)]
	public void OnClientFinishedLoading () {
		status[backstab.recConnectionId] = true;

		foreach (bool b in status) {
			if (!b) return;
		}

		Rpc("OnLoadingFinished", lastLevel);
	}

	//NOTE: Use OnNetworkLoadedLevel only if you're sure you need it.
	//If you spawn a NetScript that spawns more of itself, this loop will never terminate.
	[RpcAll(qosType = QosType.ReliableSequenced)]
	public void OnLoadingFinished (int level) {
		for (int i = 0; i < NetScript.Instances.Count; i++) {
			NetScript.Instances[i].OnNetworkLoadedLevel(level);
		}
	}
}
