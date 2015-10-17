using UnityEngine;
using System.Collections;

public class NetScriptTest : NetScript {
	public string testString = "This is a test.";
	public string boxMessage = "";
	public string broadcastMessage = "LAN Server";
	
	void Start () {
		backstab.broadcastMessage = broadcastMessage;
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

	public override void OnBackstabClientConnected (ConnectionData data) {
		boxMessage = "Client has connected.";
		Rpc("GetServerOk");
		Rpc("FlipMessageA");
		Rpc("FlipMessageB");
	}

	public override void OnBackstabClientDisconnected () {
		boxMessage = "Client has disconnected.";
	}

	public override void OnBackstabGotBroadcast () {
		boxMessage = "Got broadcast.";
	}
	
	//Rpcs
	
	[Client]
	public void GetServerOk () {
		boxMessage = "Got OK from server.";
	}
	
	[Client]
	public void FlipMessageA () {
		Debug.Log("Got message A. This should be first.");
	}
	
	[Client]
	public void FlipMessageB () {
		Debug.Log("Got message B. This should be second.");
	}
}
