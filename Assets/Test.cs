using UnityEngine;
using UnityEngine.Networking;

public class Test : MonoBehaviour {
	private int localSocketId;

	void Start () {
		NetworkTransport.Init();
		StartServer();
		StartClient();
	}

	void StartServer () {
		ConnectionConfig config = new ConnectionConfig();
		config.AddChannel(QosType.Unreliable);
		HostTopology topology = new HostTopology(config, 10);
		localSocketId = NetworkTransport.AddHost(topology, 0);
		byte err;
		NetworkTransport.StartBroadcastDiscovery(localSocketId, 8888, 1000, 1, 1, StringToBytes("Hello World"), 1024, 1000, out err);
	}

	void StartClient () {
		ConnectionConfig config = new ConnectionConfig();
		config.AddChannel(QosType.Unreliable);
		HostTopology topology = new HostTopology(config, 10);
		localSocketId = NetworkTransport.AddHost(topology, 8888);
		byte err;
		NetworkTransport.SetBroadcastCredentials(localSocketId, 1000, 1, 1, out err);
	}

	void Update () {
		int connectionId;
		int channelId;
		int receivedSize;
		byte[] msgInBuffer = new byte[1024];
		byte error;
		NetworkEventType networkEvent = NetworkEventType.DataEvent;
		
		do
		{
			networkEvent = NetworkTransport.ReceiveFromHost(localSocketId, out connectionId, out channelId, msgInBuffer, 1024, out receivedSize, out error);
			
			if (networkEvent == NetworkEventType.BroadcastEvent)
			{
				NetworkTransport.GetBroadcastConnectionMessage(localSocketId, msgInBuffer, 1024, out receivedSize, out error);
				
				string senderAddr;
				int senderPort;
				NetworkTransport.GetBroadcastConnectionInfo(localSocketId, out senderAddr, out senderPort, out error);
				Debug.Log("Got broadcast.");
				//OnReceivedBroadcast(senderAddr, BytesToString(msgInBuffer));
			}
		} while (networkEvent != NetworkEventType.Nothing);
	}

	void OnGUI () {
		GUILayout.Box("" +localSocketId);
	}

	static byte[] StringToBytes(string str)
	{
		byte[] bytes = new byte[str.Length * sizeof(char)];
		System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
		return bytes;
	}
	
	static string BytesToString(byte[] bytes)
	{
		char[] chars = new char[bytes.Length / sizeof(char)];
		System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
		return new string(chars);
	}
}
