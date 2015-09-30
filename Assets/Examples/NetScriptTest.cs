using UnityEngine;
using System.Collections;

public class NetScriptTest : NetScript {
	public string testString = "This is a test.";
	public string boxMessage = "";
	public string broadcastMessage = "LAN Server";
	
	void Start () {
		backstab.broadcastMessage = broadcastMessage;
		RegisterRpc("GetServerOk");
		//These are to verify that Backstab does not reverse the order of RPCs sent on the same frame,
		//which Unity's old networking system (from before 5.1) did.
		RegisterRpc("FlipMessageA");
		RegisterRpc("FlipMessageB");
	}

	void OnGUI () {
		if (Backstab.IsActive) {
			GUILayout.Box("Your socket ID is " +backstab.LocalSocketId);
			if (backstab.IsServer) {
				GUILayout.Box("Server active.");
			} else if (backstab.IsClient) {
				GUILayout.Box("Client active.");
			} else if (backstab.IsClient && backstab.IsConnected) {
				GUILayout.Box("Connected to server.");
			} else {
				GUILayout.Box("Not server or client.");
			}
			GUILayout.Box(boxMessage);
			foreach (ConnectionData b in backstab.broadcasters) {
				if (GUILayout.Button(b.ToString())) {
					backstab.Connect(b.address, b.port);
				}
			}
		}
	}

	public void StartServer () {
		backstab.StartServer();
	}

	public void StopServer () {
		backstab.StopServer();
	}

	public void StartClient () {
		backstab.StartClient();
	}

	public IEnumerator WaitStopServer () {
		yield return new WaitForSeconds(0);
		StopServer();
	}

	public void Disconnect () {
		backstab.Disconnect();
	}

	public void Kick () {
		backstab.Kick(0);
	}

	public override void OnBackstabConnectedToServer (ConnectionData data) {
		boxMessage = "Connected to " +data.address;
	}

	public override void OnClientConnected (ConnectionData data) {
		boxMessage = "Client has connected.";
		RpcClientsReliable("GetServerOk");
		RpcClientsReliable("FlipMessageA");
		RpcClientsReliable("FlipMessageB");
	}

	public override void OnClientDisconnected () {
		boxMessage = "Client has disconnected.";
	}

	public override void OnGotBroadcast () {
		boxMessage = "Got broadcast.";
	}
	
	//Rpcs

	public void GetServerOk () {
		boxMessage = "Got OK from server.";
	}

	public void FlipMessageA () {
		Debug.Log("Got message A. This should be first.");
	}

	public void FlipMessageB () {
		Debug.Log("Got message B. This should be second.");
	}
}
