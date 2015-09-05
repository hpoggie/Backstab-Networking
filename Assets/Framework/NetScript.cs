/*
 * IMPORTANT: You can't have more than 255 Rpcs on one NetScript.
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

	private MethodInfo[] rpcs = new MethodInfo[255];
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

	protected void RpcAllUnreliable (MethodInfo method, params System.Object[] args) {
		Backstab.RpcAllUnreliable(viewId, GetMethodId(method), args);
	}

	protected void Rpc (MethodInfo method, int playerId, params System.Object[] args) {
		RpcReliable(method, playerId, args);
	}

	protected void RpcReliable (MethodInfo method, int playerId, params System.Object[] args) {
		Backstab.RpcReliable(viewId, GetMethodId(method), args, playerId);
	}

	protected void RpcUnreliable (MethodInfo method, int playerId, params System.Object[] args) {
		Backstab.RpcUnreliable(viewId, GetMethodId(method), args, playerId);
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

	//
	//Overload redirects
	//

	protected void RegisterRpc (StringRpc function) { RegisterRpc(function.Method); }
	protected void RegisterRpc (IntRpc function) { RegisterRpc(function.Method); }
	protected void RegisterRpc (FloatRpc function) { RegisterRpc(function.Method); }
	protected void RegisterRpc (ObjectRpc function) { RegisterRpc(function.Method); }
	protected void RegisterRpc (Vector3Rpc function) { RegisterRpc(function.Method); }
	protected void RegisterRpc (System.Action function) { RegisterRpc(function.Method); }

	protected void Rpc (StringRpc function, int playerId, params System.Object[] args) { Rpc(function.Method, playerId, args); }
	protected void Rpc (IntRpc function, int playerId, params System.Object[] args) { Rpc(function.Method, playerId, args); }
	protected void Rpc (FloatRpc function, int playerId, params System.Object[] args) { Rpc(function.Method, playerId, args); }
	protected void Rpc (ObjectRpc function, int playerId, params System.Object[] args) { Rpc(function.Method, playerId, args); }
	protected void Rpc (Vector3Rpc function, int playerId, params System.Object[] args) { Rpc(function.Method, playerId, args); }
	protected void Rpc (System.Action function, int playerId, params System.Object[] args) { Rpc(function.Method, playerId, args); }

	protected void RpcAll (StringRpc function, params System.Object[] args) { RpcAll(function.Method); }
	protected void RpcAll (IntRpc function, params System.Object[] args) { RpcAll(function.Method, args); }
	protected void RpcAll (FloatRpc function, params System.Object[] args) { RpcAll(function.Method, args); }
	protected void RpcAll (ObjectRpc function, params System.Object[] args) { RpcAll(function.Method, args); }
	protected void RpcAll (Vector3Rpc function, params System.Object[] args) { RpcAll(function.Method, args); }
	protected void RpcAll (System.Action function, params System.Object[] args) { RpcAll(function.Method, args); }

	protected void Sync (StringRpc function, params System.Object[] args) { RpcAllUnreliable(function.Method, args); }
	protected void Sync (IntRpc function, params System.Object[] args) { RpcAllUnreliable(function.Method, args); }
	protected void Sync (FloatRpc function, params System.Object[] args) { RpcAllUnreliable(function.Method, args); }
	protected void Sync (ObjectRpc function, params System.Object[] args) { RpcAllUnreliable(function.Method, args); }
	protected void Sync (Vector3Rpc function, params System.Object[] args) { RpcAllUnreliable(function.Method, args); }
	protected void Sync (System.Action function, params System.Object[] args) { RpcAllUnreliable(function.Method, args); }

}
