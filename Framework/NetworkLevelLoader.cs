using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class NetworkLevelLoader : NetScript {
	static int lastLevel = -108;

	bool[] status;
	bool hasServerLoaded = false;

	public void Start () {
		DontDestroyOnLoad(this);
	}

	public void AllLoadLevel (int level) {
		if (!backstab.IsServer) {
			Debug.LogError("Can't issue LoadLevel messages if not the server.");
			return;
		}
		status = new bool[backstab.NumConnections];
		Rpc("LoadLevel", level);
		LoadLevel(level);
	}

	[RpcClients(qosType = QosType.ReliableSequenced)]
	private void LoadLevel (int level) {
		SceneManager.LoadScene(level);
	}

	void OnLevelWasLoaded (int level) {
		if (backstab.IsClient) {
			Rpc("OnClientFinishedLoading");
		} else {
			hasServerLoaded = true;

			if (HasEveryoneLoaded()) {
				Rpc("OnLoadingFinished", lastLevel);
				OnLoadingFinished(lastLevel);
			}
		}
		lastLevel = level;
	}

	[RpcServer(qosType = QosType.ReliableSequenced)]
	public void OnClientFinishedLoading () {
		status[backstab.recConnectionId] = true;

		if (HasEveryoneLoaded()) {
			Rpc("OnLoadingFinished", lastLevel);
		}
	}

	public bool HasEveryoneLoaded () {
		if (!hasServerLoaded) return false;
		if (status == null || status.Length == 0) return true;

		foreach (bool b in status) {
			if (!b) return false;
		}

		return true;
	}

	//NOTE: Use OnNetworkLoadedLevel only if you're sure you need it.
	//If you spawn a NetScript that spawns more of itself, this loop will never terminate.
	[RpcClients(qosType = QosType.ReliableSequenced)]
	public void OnLoadingFinished (int level) {
		for (int i = 0; i < NetScript.Instances.Count; i++) {
			if (NetScript.Find(i) != null) {
				NetScript.Find(i).OnNetworkLoadedLevel(level);
			}
		}
	}
}
