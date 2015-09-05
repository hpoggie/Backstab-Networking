using UnityEngine;
using System.Collections;

//
//A synchronized transform without prediction.
//

public class NetTransform : NetScript {
	// Use this for initialization
	void Start () {
		RegisterRpc(SyncPosition);
	}
	
	// Update is called once per frame
	void Update () {
		if (Backstab.IsServer) {
			RpcAll(SyncPosition, transform.position.x, transform.position.y, transform.position.z);
		}
	}

	public void SyncPosition (float x, float y, float z) {
		transform.position = new Vector3(x, y, z);
	}

}
