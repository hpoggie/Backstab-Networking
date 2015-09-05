using UnityEngine;
using System.Collections;

//
//A synchronized transform without prediction.
//

public class NetTransform : NetScript {
	void Start () {
		RegisterRpc(SyncPosition);
		RegisterRpc(SyncRotation);
		RegisterRpc(SyncScale);
		InvokeRepeating("Sync", 0.1f, 0.1f);
	}
	
	void Sync () {
		if (Backstab.IsServer) {
			RpcAll(SyncPosition, transform.position.x, transform.position.y, transform.position.z);
			RpcAll(SyncRotation, transform.rotation.w, transform.rotation.x, transform.rotation.y, transform.rotation.z);
			RpcAll(SyncScale, transform.localScale.x, transform.localScale.y, transform.localScale.z);
		}
	}

	public void SyncPosition (float x, float y, float z) {
		transform.position = new Vector3(x, y, z);
	}

	public void SyncRotation (float w, float x, float y, float z) {
		transform.rotation = new Quaternion(x, y, z, w);
	}

	public void SyncScale (float x, float y, float z) {
		transform.localScale = new Vector3(x, y, z);
	}

}
