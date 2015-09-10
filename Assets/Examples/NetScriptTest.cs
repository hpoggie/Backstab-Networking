using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Reflection;

public class NetScriptTest : NetScript {
	public string testString = "This is a test.";
	public string boxMessage = "";
	
	void Start () {
		Backstab.Init();
		//RegisterRpc(SaySomething);
		RegisterRpc("GetServerOk");
	}

	void OnGUI () {
		if (Backstab.IsActive) {
			GUILayout.Box("Your socket ID is " +Backstab.LocalSocketId);
			if (Backstab.IsServer) {
				GUILayout.Box("Server active.");
			} else if (Backstab.IsClient) {
				GUILayout.Box("Connected to server.");
			} else {
				GUILayout.Box("Not connected.");
			}
			GUILayout.Box(boxMessage);
		}
	}

	public void StartServer () {
		Backstab.StartServer();
	}

	public void Connect () {
		Backstab.Connect("127.0.0.1");
	}

	public void Disconnect () {
		Backstab.Disconnect();
	}

	public override void OnConnectedToServer () {
		boxMessage = "Connected to server.";
		//Rpc(SaySomething, Backstab.serverConnectionId, "Hello World");
	}

	public override void OnClientConnected () {
		boxMessage = "Client has connected.";
		RpcAllReliable("GetServerOk");
	}

	//
	//Rpcs
	//
	/*
	public void SaySomething (string str) {
		Debug.Log(str);
	}
	*/
	public void GetServerOk () {
		boxMessage = "Got OK from server.";
	}
}
