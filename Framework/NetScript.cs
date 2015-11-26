/*
 * IMPORTANT: You can't have more than 256 Rpcs on one NetScript.
 */

using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Reflection;
using System;

[AttributeUsage(AttributeTargets.Method)]
public class RpcAttribute : Attribute {
	public UnityEngine.Networking.QosType qosType = UnityEngine.Networking.QosType.Reliable;

	public RpcAttribute () { }
	public RpcAttribute (QosType qosType) { this.qosType = qosType; }
}
public class RpcClientsAttribute : RpcAttribute { }
public class RpcAllAttribute : RpcAttribute { }
public class RpcServerAttribute : RpcAttribute { }
public class InputCommandAttribute : RpcAttribute { }

public class NetScript : MonoBehaviour {
	private static List<NetScript> instances = new List<NetScript>();
	public static List<NetScript> Instances { get { return instances; } }
	public static float syncInterval = 0.1f;
	
	public Backstab backstab;
	
	private int viewId;
	public int ViewId { get { return viewId; } }
	private MethodInfo[] rpcs = new MethodInfo[256];
	private byte currentSize = 0;
	
	private BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	void Awake () {
		viewId = instances.Count;
		instances.Add(this);
		if (!backstab) backstab = FindObjectOfType<Backstab>();
		
		foreach (MethodInfo m in GetType().GetMethods(flags)) {
			foreach (Attribute a in m.GetCustomAttributes(true)) {
				RpcAttribute r = a as RpcAttribute;
				if (r != null) {
					RegisterRpc(m);
				}
			}
		}

		InvokeRepeating("OnSync", syncInterval, syncInterval);
	}
	/*
	void OnDestroy () {
		instances.Remove(this);
		foreach (NetScript n in instances) {
			if (n.viewId > viewId) {
				n.viewId--;
			}
		}
	}
	*/
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

	private void RegisterRpc (MethodInfo method) {
		if (method != null) {
			rpcs[currentSize] = method;
			currentSize++;
		} else {
			Debug.LogError("Null method passed to RegisterRpc. Make sure your Rpcs are public.");
		}
	}
	
	//Rpc

	protected void Rpc (string fname, params System.Object[] args) {
		byte index = GetMethodIndex(fname);
		QosType qosType = QosType.Reliable;

		foreach (Attribute a in rpcs[index].GetCustomAttributes(true)) {
			if (a is RpcAttribute) {
				qosType = (a as RpcAttribute).qosType;
				if (a is RpcServerAttribute && backstab.IsClient) {
					RpcServer(index, qosType, args);
				} else if (a is RpcClientsAttribute && backstab.IsServer) {
					RpcClients(index, qosType, args);
				} else if (a is RpcAllAttribute && backstab.IsServer) {
					rpcs[index].Invoke(this, args);
					RpcClients(index, qosType, args);
				} else if (a is InputCommandAttribute && backstab.IsServer) {
					rpcs[index].Invoke(this, args);
					RpcClients(index, qosType, args);
				} else if (a is InputCommandAttribute && backstab.IsClient) {
					RpcServer(index, qosType, args);
				}
			}
		}
	}
	
	protected void RpcClients (string fname, params object[] args) {
		byte index = GetMethodIndex(fname);
		QosType qosType = QosType.Reliable;

		foreach (Attribute a in rpcs[index].GetCustomAttributes(true)) {
			if (a is RpcAttribute) qosType = (a as RpcAttribute).qosType;
		}

		RpcClients(index, qosType, args);
	}

	protected void RpcServer (string fname, params object[] args) {
		byte index = GetMethodIndex(fname);
		QosType qosType = QosType.Reliable;
		
		foreach (Attribute a in rpcs[index].GetCustomAttributes(true)) {
			if (a is RpcAttribute) qosType = (a as RpcAttribute).qosType;
		}
		
		RpcServer(index, qosType, args);
	}
	
	protected void RpcSpecific (string fname, int connectionId, params object[] args) {
		byte index = GetMethodIndex(fname);
		QosType qosType = QosType.Reliable;

		foreach (Attribute a in rpcs[index].GetCustomAttributes(true)) {
			if (a is RpcAttribute) qosType = (a as RpcAttribute).qosType;
		}

		backstab.Rpc(viewId, GetMethodIndex(fname), qosType, connectionId, args);
	}

	protected void RpcClients (byte methodId, QosType qosType, params System.Object[] args) { backstab.RpcAll(viewId, methodId, qosType, args); }

	protected void RpcServer (byte methodId, QosType qosType, params System.Object[] args) {
		if (backstab.IsServer) {
			Debug.LogError("Can't send to server if already server.");
			return;
		} 
		backstab.Rpc(viewId, methodId, qosType, 1, args);
	}
	
	//Recieving

	public void RecieveRpc (RpcData rpc) {
		MethodInfo m = rpcs[rpc.methodId];
		InputCommandAttribute attr = null;

		foreach (Attribute a in m.GetCustomAttributes(true)) {
			if (a is RpcServerAttribute && !backstab.IsServer) {
				Debug.LogError("Can't recieve server Rpcs if not the server. " +m.Name);
				return;
			}
			if (a is RpcClientsAttribute && !backstab.IsClient) {
				Debug.LogError("Can't recieve client Rpcs if not a client. " +m.Name);
				return;
			}
			if (a is InputCommandAttribute && backstab.IsServer) {
				attr = (a as InputCommandAttribute);
			}
		}
		
		try {
			m.Invoke(this, rpc.args);
		} catch (TargetParameterCountException e) {
			Debug.LogError(e.GetType().ToString() + " when attempting to call " +m.Name + " Expected " +m.GetParameters().Length + " parameters; Actual " +rpc.args.Length);
		} catch (Exception e) {
			Debug.Log(e.GetType().ToString() + " when attempting to call " +m.Name);
		}

		if (attr != null) {
			RpcClients(rpc.methodId, attr.qosType, rpc.args);
		}
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

	public static NetScript Find (int viewId) {
		if (viewId > instances.Count || viewId < 0) return null;
		else return instances[viewId];
	}
}
