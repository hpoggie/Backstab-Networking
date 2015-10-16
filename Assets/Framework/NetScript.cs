/*
 * IMPORTANT: You can't have more than 256 Rpcs on one NetScript.
 */

using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;

[AttributeUsage(AttributeTargets.Method)]
public class RpcAttribute : System.Attribute {
}

public class NetScript : MonoBehaviour {
	public static int currentId = 0;
	public static List<NetScript> instances = new List<NetScript>();
	public static float syncInterval = 0.1f;
	
	public Backstab backstab;
	
	private int viewId;
	public int ViewId { get { return viewId; } }
	private MethodInfo[] rpcs = new MethodInfo[256];
	private byte currentSize = 0;
	
	private BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	void Awake () {
		instances.Add(this);
		if (!backstab) backstab = FindObjectOfType<Backstab>();
		viewId = currentId;
		currentId++;
		
		foreach (MethodInfo m in GetType().GetMethods(flags)) {
			foreach (Attribute a in m.GetCustomAttributes(true)) {
				RpcAttribute r = a as RpcAttribute;
				if (r != null) {
					RegisterRpc(m);
				}
			}
		}
	}
	
	public virtual void OnBackstabAwake () {
		if (!backstab && Backstab.instances.Count > 0) backstab = Backstab.instances[0];
		InvokeRepeating("OnSync", syncInterval, syncInterval);
	}
	
	void OnDestroy () {
		instances.Remove(this);
	}
	
	public virtual void OnBackstabStartServer () { ; }
	public virtual void OnBackstabStartClient () { ; }
	public virtual void OnBackstabStopServer () { ; }
	
	public virtual void OnBackstabConnectedToServer (ConnectionData data) { ; }
	public virtual void OnBackstabClientConnected (ConnectionData data) { ; }
	public virtual void OnBackstabDisconnectedFromServer () { ; }
	public virtual void OnBackstabClientDisconnected () { ; }
	public virtual void OnBackstabFailedToConnect () { ; }
	
	public virtual void OnBackstabGotBroadcast () { ; }
	protected virtual void OnSync () { ; }

	//Registering

	protected void RegisterRpc (MethodInfo method) {
		if (method != null) {
			rpcs[currentSize] = method;
			currentSize++;
		} else {
			Debug.LogError("Null method passed to RegisterRpc. Make sure your Rpcs are public.");
		}
	}

	protected void RegisterRpc (string fname) {
		RegisterRpc(GetType().GetMethod(fname));
	}
	
	//Rpc

	protected void Rpc (string fname, int playerId, params System.Object[] args) { RpcReliable(GetMethodIndex(fname), playerId, args); }
	protected void RpcReliable (string fname, int playerId, params System.Object[] args) { RpcReliable(GetMethodIndex(fname), playerId, args); }
	protected void RpcReliable (byte methodId, int playerId, params System.Object[] args) { backstab.RpcReliable(viewId, methodId, args, playerId); }
	protected void RpcUnreliable (byte methodId, int playerId, params System.Object[] args) { backstab.RpcUnreliable(viewId, methodId, args, playerId); }
	
	//RpcAll

	protected void RpcClients (string fname, params System.Object[] args) { RpcClientsReliable(GetMethodIndex(fname), args); }
	protected void RpcClientsReliable (string fname, params System.Object[] args) { RpcClientsReliable(GetMethodIndex(fname), args); }
	protected void RpcClientsReliable (byte methodId, params System.Object[] args) { backstab.RpcAllReliable(viewId, methodId, args); }
	protected void RpcClientsUnreliable (string fname, params System.Object[] args) { RpcClientsUnreliable(GetMethodIndex(fname), args); }
	protected void RpcClientsUnreliable (byte methodId, params System.Object[] args) { backstab.RpcAllUnreliable(viewId, methodId, args); }
	
	//RpcServer

	protected void RpcServer (string fname, params System.Object[] args) {
		if (backstab.IsServer) {
			Debug.LogError("Can't send to server if already server.");
			return;
		} 
		RpcReliable(GetMethodIndex(fname), 1, args);
	}
	
	//Recieving

	public void RecieveRpc (RpcData rpc) {
		rpcs[rpc.methodId].Invoke(this, rpc.args);
	}

	public byte GetMethodIndex (string s) {
		byte i;
		for (i = 0; i < currentSize; i++) {
			if (rpcs[i].Name == s) {
				return i;
			}
		}
		Debug.LogError("Method not found. Defaulting to first method in array.");
		return 0;
	}
	
}
