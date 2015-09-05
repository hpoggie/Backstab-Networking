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
	}
	
	protected override void OnSync () {
		if (Backstab.IsServer) {
			Sync(SyncPosition, transform.position.x, transform.position.y, transform.position.z);
			Sync(SyncRotation, transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
			Sync(SyncScale, transform.localScale.x, transform.localScale.y, transform.localScale.z);
		}
	}

	public void SyncPosition (float x, float y, float z) { transform.position = new Vector3(x, y, z); }
	public void SyncRotation (float x, float y, float z) { transform.rotation = Quaternion.Euler(x, y, z); }
	public void SyncScale (float x, float y, float z) { transform.localScale = new Vector3(x, y, z); }

}
