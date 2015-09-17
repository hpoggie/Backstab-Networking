using UnityEngine;
using System.Collections;

public class NetScriptTest : NetScript {
	public string testString = "This is a test.";
	public string boxMessage = "";
	
	void Start () {
		Backstab.Init();
		RegisterRpc("GetServerOk");
	}

	void OnGUI () {
		if (Backstab.IsActive) {
			GUILayout.Box("Your socket ID is " +Backstab.LocalSocketId);
			if (Backstab.IsServer) {
				GUILayout.Box("Server active.");
			} else if (Backstab.IsClient) {
				GUILayout.Box("Client active.");
			} else if (Backstab.IsClient && Backstab.IsConnected) {
				GUILayout.Box("Connected to server.");
			} else {
				GUILayout.Box("Not server or client.");
			}
			GUILayout.Box(boxMessage);
			foreach (ConnectionData b in Backstab.broadcasters) {
				if (GUILayout.Button(b.ToString())) {
					Backstab.Connect(b.address, b.port);
				}
			}
		}
	}

	public void StartServer () {
		Backstab.StartServer();
	}

	public void StopServer () {
		Backstab.StopServer();
	}

	public void StartClient () {
		Backstab.StartClient();
	}

	public IEnumerator WaitStopServer () {
		yield return new WaitForSeconds(0);
		StopServer();
	}

	public void Disconnect () {
		Backstab.Disconnect();
	}

	public void Kick () {
		Backstab.Kick(0);
	}

	public override void OnConnectedToServer () {
		boxMessage = "Connected to server.";
	}

	public override void OnClientConnected () {
		boxMessage = "Client has connected.";
		RpcClientsReliable("GetServerOk");
	}

	public override void OnGotBroadcast () {
		boxMessage = "Got broadcast.";
	}
	
	//Rpcs

	public void GetServerOk () {
		boxMessage = "Got OK from server.";
	}
}
