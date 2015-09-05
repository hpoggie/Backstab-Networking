using UnityEngine;
using System.Collections;

public class NetRigidbody : NetScript {

	void Start () {
		RegisterRpc(SyncPosition);
		RegisterRpc(SyncRotation);
		RegisterRpc(SyncVelocity);
		RegisterRpc(SyncAngularVelocity);
	}
	
	protected override void OnSync () {
		if (Backstab.IsServer) {
			Sync(SyncPosition, transform.position.x, transform.position.y, transform.position.z);
			Sync(SyncRotation, transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
			Sync(SyncVelocity, GetComponent<Rigidbody>().velocity.x, GetComponent<Rigidbody>().velocity.y, GetComponent<Rigidbody>().velocity.z);
			Sync(SyncAngularVelocity, GetComponent<Rigidbody>().angularVelocity.x, GetComponent<Rigidbody>().angularVelocity.y, GetComponent<Rigidbody>().angularVelocity.z);
		}
	}

	public void SyncPosition (float x, float y, float z) { transform.position = new Vector3(x, y, z); }
	public void SyncRotation (float x, float y, float z) { transform.rotation = Quaternion.Euler(x, y, z); }
	public void SyncVelocity (float x, float y, float z) { GetComponent<Rigidbody>().velocity = new Vector3(x, y, z); }
	public void SyncAngularVelocity (float x, float y, float z) { GetComponent<Rigidbody>().angularVelocity = new Vector3(x, y, z); }
}
