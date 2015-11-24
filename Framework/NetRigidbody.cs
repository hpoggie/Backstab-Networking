/*
 * A rigidbody syncing script with a sort of pseudo-prediction.
 * The client keeps calculating the physics updates as usual and changes to whatever the server says.
 * This gets around Unity's lack of state storage, but has the downside that the client will always lag begind the server.
 * If you're on fast internet it shouldn't be a problem, especially since Backstab is designed to minimize bandwidth use.
 */

using UnityEngine;

[System.Serializable]
public class NetVector3 {
	public float x;
	public float y;
	public float z;

	public NetVector3 (Vector3 v) {
		this.x = v.x;
		this.y = v.y;
		this.z = v.z;
	}

	public Vector3 Get () {
		return new Vector3(x, y, z);
	}
}

public class NetRigidbody : NetScript {
	protected override void OnSync () {
		if (backstab.IsServer) {
			Rpc("SyncPosition", new NetVector3(transform.position));
			Rpc("SyncRotation", new NetVector3(transform.rotation.eulerAngles));
			Rpc("SyncVelocity", new NetVector3(GetComponent<Rigidbody>().velocity));
			Rpc("SyncAngularVelocity", new NetVector3(GetComponent<Rigidbody>().angularVelocity));
		}
	}
	
	[RpcClients] public void SyncPosition (NetVector3 pos) { transform.position = pos.Get(); }
	[RpcClients] public void SyncRotation (NetVector3 rot) { transform.rotation = Quaternion.Euler(rot.Get()); }
	[RpcClients] public void SyncVelocity (NetVector3 v) { GetComponent<Rigidbody>().velocity = v.Get(); }
	[RpcClients] public void SyncAngularVelocity (NetVector3 av) { GetComponent<Rigidbody>().angularVelocity = av.Get(); }
}
