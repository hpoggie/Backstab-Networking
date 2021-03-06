﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Chat : NetScript {
	public string text = "Type message here.";
	public string typingBox = "";
	public int maxLog = 10;
	public double messageLifetime = 5;
	private string[] log;
	private double lastTime;
	private bool isTyping = false;

	void Start () {
		log = new string[maxLog];
	}

	void Update () {
		if (Time.realtimeSinceStartup - lastTime > messageLifetime) {
			lastTime = Time.realtimeSinceStartup;
			for (int i = 0; i < log.Length; i++) {
				if (log[i] != null) {
					log[i] = null;
					break;
				}
			}
		}
	}

	void OnGUI () {
		GUILayout.BeginArea(new Rect(Screen.width / 2 - 150, 0, 300, 500));
		if (log.Length > 0) {
			foreach (string s in log) {
				if (s != null) {
					GUILayout.Box(s);
				}
			}
		}
		if (isTyping) {
			GUILayout.Box("-----------Type Below-----------");
			GUILayout.Box(text);
		}

		if (typingBox.Length > 0) GUILayout.Box(typingBox);

		GUILayout.EndArea();

		GetTyping();
	}

	void GetTyping () {
		if (Event.current == null || Event.current.type != EventType.KeyDown) return;
		char c = Event.current.character;
		if (Event.current.keyCode == KeyCode.Return) {
			if (isTyping && text.Length > 1) {
				Log("Local: " +text);
				if (backstab.IsServer) RpcClients("RemoteLog", text);
				else if (backstab.IsClient) RpcServer("RemoteLog", text);
			}
			text = "";
			isTyping = !isTyping;
		} else if (isTyping && Event.current.keyCode == KeyCode.Backspace && text.Length > 1) {
			text = text.Substring(0, text.Length - 2);
			if (backstab.IsServer) RpcClients("UpdateText", text);
			else if (backstab.IsClient) RpcServer("UpdateText", text);
		} else if (isTyping && c != '\n') {
			text += c;
			if (backstab.IsServer) RpcClients("UpdateText", text);
			else if (backstab.IsClient) RpcServer("UpdateText", text);
		}
	}

	[Rpc]
	public void RemoteLog (string s) {
		Log("Con. " +backstab.recConnectionId +": " +s);
	}

	[Rpc]
	public void UpdateText (string s) {
		typingBox = s;
	}

	public void Log (string s) {
		for (int i = 0; i < log.Length - 1; i++) {
			log[i] = log[i+1];
		}
		log[log.Length - 1] = s;
	}
}
