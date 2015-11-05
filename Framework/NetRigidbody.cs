/*
 * A rigidbody syncing script with a sort of pseudo-prediction.
 * The client keeps calculating the physics updates as usual and changes to whatever the server says.
 * This gets around Unity's lack of state storage, but has the downside that the client will always lag begind the server.
 * If you're on fast internet it shouldn't be a problem, especially since Backstab is designed to minimize bandwidth use.
 */

using UnityEngine;
using System.Collections;

public class NetRigidbody : NetScript {
	protected override void OnSync () {
		if (backstab.IsServer) {
			Rpc("SyncPosition", transform.position.x, transform.position.y, transform.position.z);
			Rpc("SyncRotation", transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
			Rpc("SyncVelocity", GetComponent<Rigidbody>().velocity.x, GetComponent<Rigidbody>().velocity.y, GetComponent<Rigidbody>().velocity.z);
			Rpc("SyncAngularVelocity", GetComponent<Rigidbody>().angularVelocity.x, GetComponent<Rigidbody>().angularVelocity.y, GetComponent<Rigidbody>().angularVelocity.z);
		}
	}
	
	[RpcClients] public void SyncPosition (float x, float y, float z) { transform.position = new Vector3(x, y, z); }
	[RpcClients] public void SyncRotation (float x, float y, float z) { transform.rotation = Quaternion.Euler(x, y, z); }
	[RpcClients] public void SyncVelocity (float x, float y, float z) { GetComponent<Rigidbody>().velocity = new Vector3(x, y, z); }
	[RpcClients] public void SyncAngularVelocity (float x, float y, float z) { GetComponent<Rigidbody>().angularVelocity = new Vector3(x, y, z); }
}
