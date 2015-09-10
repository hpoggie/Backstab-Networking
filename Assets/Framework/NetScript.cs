/*
 * IMPORTANT: You can't have more than 256 Rpcs on one NetScript.
 */

using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

public class NetScript : MonoBehaviour {
	public static int currentId = 0;
	public static List<NetScript> instances = new List<NetScript>();

	public static float syncInterval = 0.1f;
	
	private int viewId;
	public int ViewId { get { return viewId; } }

	private MethodInfo[] rpcs = new MethodInfo[256];
	private byte currentSize = 0;

	public delegate void StringRpc (string s);
	public delegate void IntRpc (int i);
	public delegate void FloatRpc (float f);
	public delegate void ObjectRpc (System.Object ob);
	public delegate void Vector3Rpc (float x, float y, float z);

	void Awake () {
		instances.Add(this);
		viewId = currentId;
		currentId++;
		InvokeRepeating("OnSync", syncInterval, syncInterval);
	}

	void OnDestroy () {
		instances.Remove(this);
	}

	public virtual void OnConnected () { ; }
	public virtual void OnDisconnected () { ; }
	protected virtual void OnSync () { ; }

	protected void RegisterRpc (MethodInfo method) {
		rpcs[currentSize] = method;
		currentSize++;
	}

	protected void RpcAll (MethodInfo method, params System.Object[] args) {
		RpcAllReliable(method, args);
	}

	protected void RpcAllReliable (MethodInfo method, params System.Object[] args) {
		Backstab.RpcAllReliable(viewId, GetMethodId(method), args);
	}

	protected void RpcAllReliable (byte methodId, params System.Object[] args) {
		Backstab.RpcAllReliable(viewId, methodId, args);
	}

	protected void RpcAllUnreliable (MethodInfo method, params System.Object[] args) {
		Backstab.RpcAllUnreliable(viewId, GetMethodId(method), args);
	}

	protected void RpcAllUnreliable (byte methodId, params System.Object[] args) {
		Backstab.RpcAllUnreliable(viewId, methodId, args);
	}

	protected void Rpc (MethodInfo method, int playerId, params System.Object[] args) {
		RpcReliable(method, playerId, args);
	}

	protected void RpcReliable (MethodInfo method, int playerId, params System.Object[] args) {
		Backstab.RpcReliable(viewId, GetMethodId(method), args, playerId);
	}

	protected void RpcReliable (byte methodId, int playerId, params System.Object[] args) {
		Backstab.RpcReliable(viewId, methodId, args, playerId);
	}

	protected void RpcUnreliable (MethodInfo method, int playerId, params System.Object[] args) {
		Backstab.RpcUnreliable(viewId, GetMethodId(method), args, playerId);
	}
	
	protected void RpcUnreliable (byte methodId, int playerId, params System.Object[] args) {
		Backstab.RpcUnreliable(viewId, methodId, args, playerId);
	}

	public void RecieveRpc (RpcData rpc) {
		rpcs[rpc.methodId].Invoke(this, rpc.args);
	}

	private byte GetMethodId (MethodInfo method) {
		for (byte i = 0; i < currentSize; i++) {
			if (rpcs[i] == method) {
				return i;
			}
		}
		Debug.LogError("Method not found. Defaulting to first method in array.");
		return 0;
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

	//
	//Overload redirects
	//

	protected void RegisterRpc (StringRpc function) { RegisterRpc(function.Method); }
	protected void RegisterRpc (IntRpc function) { RegisterRpc(function.Method); }
	protected void RegisterRpc (FloatRpc function) { RegisterRpc(function.Method); }
	protected void RegisterRpc (ObjectRpc function) { RegisterRpc(function.Method); }
	protected void RegisterRpc (Vector3Rpc function) { RegisterRpc(function.Method); }
	protected void RegisterRpc (System.Action function) { RegisterRpc(function.Method); }

	protected void RpcReliable (string fname, int playerId, params System.Object[] args) { RpcReliable(GetMethodIndex(fname), playerId, args); }

	protected void RpcAllReliable (string fname, params System.Object[] args) { RpcAllReliable(GetMethodIndex(fname), args); }
	protected void RpcAllUnreliable (string fname, params System.Object[] args) { RpcAllUnreliable(GetMethodIndex(fname), args); }

}
