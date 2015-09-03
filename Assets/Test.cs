using UnityEngine;
using UnityEngine.Networking;

public class Test : MonoBehaviour {
	private int localSocketId;
	
	void Start () {
		NetworkTransport.Init();
		HostTopology topology = new HostTopology(new ConnectionConfig(), 10);
		localSocketId = NetworkTransport.AddHost(topology, 8888);
	}

	void OnGUI () {
		GUILayout.Box("" +localSocketId);
	}
}
