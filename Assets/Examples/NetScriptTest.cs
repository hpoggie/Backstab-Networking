using UnityEngine;
using System.Collections;
using System.Reflection;

public class NetScriptTest : NetScript {
	public string testString = "This is a test.";
	public string boxMessage = "";
	
	void SetSocketId (int i) {
		Backstab.localSocketId = i;
	}

	void Init () {
		Backstab.Init();
		RegisterRpc(SaySomething);
	}

	void OnGUI () {
		if (Backstab.IsActive) {
			GUILayout.Box("Your socket is " +Backstab.LocalSocketId);
			if (Backstab.IsServer) {
				GUILayout.Box("Server active.");
			}
			GUILayout.Box(boxMessage);
		} else {
			SetSocketId(int.Parse(GUILayout.TextField(Backstab.localSocketId.ToString())));
			if (GUILayout.Button("Start")) {
				Init();
			}
		}
	}

	public void StartServer () {
		Backstab.StartServer();
	}

	public void Connect () {
		Backstab.Connect("127.0.0.1");
	}

	public override void OnConnected () {
		boxMessage = "Connected to server.";
		//Backstab.Send(new Message(ViewId, testString));
		Rpc(SaySomething, Backstab.serverConnectionId, "Hello World");
	}

	public override void OnGotMessage (System.Object message) {
		boxMessage = message as string;
	}

	public void OnClientConnected () {
		boxMessage = "Client has connected.";
	}

	//
	//Rpcs
	//

	public void SaySomething (string str) {
		Debug.Log(str);
	}
}
