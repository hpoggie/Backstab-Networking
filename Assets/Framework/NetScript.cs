/*
 * IMPORTANT: You can't have more than 255 Rpcs on one NetScript.
 */

using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

public class NetScript : MonoBehaviour {
	public static int currentId = 0;
	public static List<NetScript> instances = new List<NetScript>();

	public static float checkViewIdInterval = 5.0f;
	
	private int viewId;
	public int ViewId { get { return viewId; } }

	private MethodInfo[] rpcs = new MethodInfo[255];
	private byte currentSize = 0;

	public delegate void StringRpc (string s);
	public delegate void IntRpc (int i);
	public delegate void FloatRpc (float f);
	public delegate void ObjectRpc (System.Object ob);

	void Awake () {
		instances.Add(this);
		viewId = currentId;
		currentId++;

		//RegisterRpc(VerifyViewId);
		//InvokeRepeating("CheckViewId", 0.0f, checkViewIdInterval);
	}

	void OnDestroy () {
		instances.Remove(this);
	}

	public virtual void OnGotPacket (System.Object packet) {
		;
	}
	
	public virtual void OnGotMessage (System.Object message) {
		;
	}
	
	public virtual void OnConnected () {
		;
	}
	
	public virtual void OnDisconnected () {
		;
	}

	//
	//ID Checking
	//
	/*
	void CheckViewId () {
		if (Backstab.IsActive) {
			Rpc(VerifyViewId, viewId);
		}
	}

	void VerifyViewId (int id) {
		if (viewId != id) {
			Debug.LogError("Inconsistent viewIds.");
		}
	}
	*/
	//
	//Rpcs
	//

	protected void RegisterRpc (MethodInfo method) {
		rpcs[currentSize] = method;
		currentSize++;
	}

	protected void Rpc (MethodInfo method, int playerId, params System.Object[] args) {
		for (byte i = 0; i < currentSize; i++) {
			if (rpcs[i] == method) {
				Backstab.Rpc(viewId, i, args, playerId);
			}
		}
	}

	public void RecieveRpc (RpcData rpc) {
		rpcs[rpc.methodId].Invoke(this, rpc.args);
	}

	//
	//Overload redirects
	//

	protected void RegisterRpc (StringRpc function) { RegisterRpc(function.Method); }
	protected void RegisterRpc (IntRpc function) { RegisterRpc(function.Method); }
	protected void RegisterRpc (FloatRpc function) { RegisterRpc(function.Method); }
	protected void RegisterRpc (ObjectRpc function) { RegisterRpc(function.Method); }
	protected void RegisterRpc (System.Action function) { RegisterRpc(function.Method); }

	protected void Rpc (StringRpc function, int playerId, params System.Object[] args) { Rpc(function.Method, playerId, args); }
	//protected void Rpc (IntRpc function, params System.Object[] args) { Rpc(function.Method, args); }
	//protected void Rpc (FloatRpc function, params System.Object[] args) { Rpc(function.Method, args); }
	//protected void Rpc (ObjectRpc function, params System.Object[] args) { Rpc(function.Method, args); }
	//protected void Rpc (System.Action function, params System.Object[] args) { Rpc(function.Method, args); }

}
