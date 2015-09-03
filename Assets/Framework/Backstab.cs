/*
 * How to use
 * 
 * Before doing anything, call Backstab.Init().
 * To start a server, call Backstab.StartServer().
 * To connect to a server, call Backstab.Connect(someIp), where someIp is the ip address of the server.
 * To send a message, call Backstab.Send(myMessage), where myMessage is the message you want to send.
 * To quit, call Backstab.Quit().
 * 
 */

using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;

[System.Serializable]
public class Message {
	public int sceneId;
	public System.Object message;

	public Message (int sceneId, System.Object message) {
		this.sceneId = sceneId;
		this.message = message;
	}
}

[System.Serializable]
public class RpcData {
	public int sceneId;
	public int methodId;
	public System.Object[] args;

	public RpcData (int sceneId, int methodId, System.Object[] args) {
		this.sceneId = sceneId;
		this.methodId = methodId;
		this.args = args;
	}
}

public class Backstab : MonoBehaviour {
	private static Backstab instance;
	public static bool IsActive { get { return (instance != null); } }

	public static int port = 8888;
	public static int maxConnections = 10;

	public static int localSocketId = 0; //The socket id of this computer. You can have multiple sockets open with NetworkTransport, but Backstab only uses one.
	public static int LocalSocketId { get { return localSocketId; } }
	public static int serverConnectionId;
	public static int[] clientSocketIds;
	public static int numConnections = 0;
	public static bool IsConnected { get { return isServer; }  }
	private static int connectionId; //Used to tell message recievers which connection sent the message
	private static int reliableChannelId;
	private static int unreliableChannelId;

	private static bool isServer;
	public static bool IsServer { get { return isServer; } }

	//
	//Basic functions
	//

	public static void Init () {
		if (instance == null) {
			NetworkTransport.Init();
			MakeInstance();
		} else {
			Debug.LogError("Backstab is already initialized.");
		}
	}

	public static void Quit () {
		Disconnect();
		isServer = false;
		NetworkTransport.Shutdown();
	}

	public static void StartServer () {
		if (!IsConnected) {
			isServer = true;
			OpenSocket(port);
		} else {
			Debug.LogError("Can't become a server if already connected.");
		}
	}

	//Warning: HEAVY WIZARDRY. I do not know what all of these arguments do.
	public static void Connect (string ip) {
		OpenSocket(0);
		byte error;
		connectionId = NetworkTransport.Connect(localSocketId, ip, port, 0, out error);
	}

	public static void Disconnect () {
		byte error;
		NetworkTransport.Disconnect(localSocketId, connectionId, out error);
	}

	//
	//Sending
	//

	public static void Rpc (int viewId, byte methodId, System.Object[] args, int connectionId) {
		SendReliable(new RpcData(viewId, methodId, args), connectionId);
	}
	/*
	public static void Send (Message message) {
		SendReliable(message);
	}
	*/
	public static void SendReliable (System.Object packet, int targetId) {
		Send(packet, reliableChannelId, targetId);
	}
	
	public static void SendUnreliable (System.Object packet, int targetId) {
		Send(packet, unreliableChannelId, targetId);
	}
	
	public static void Send (System.Object packet, int channelId, int targetId) {
		byte error;
		byte[] buffer = new byte[1024];
		Stream stream = new MemoryStream(buffer);
		BinaryFormatter formatter = new BinaryFormatter();
		formatter.Serialize(stream, packet);
		NetworkTransport.Send(localSocketId, targetId, channelId, buffer, buffer.Length, out error);
	}
	
	//
	//Private functions
	//

	private static void OpenSocket (int socketPort) {
		ConnectionConfig config = new ConnectionConfig();
		reliableChannelId = config.AddChannel(QosType.Reliable);
		unreliableChannelId = config.AddChannel(QosType.Unreliable);
		ConnectionConfig.Validate(config);

		clientSocketIds = new int[maxConnections];
		HostTopology topology = new HostTopology(config, maxConnections);
		localSocketId = NetworkTransport.AddHost(topology, socketPort, null);
		Debug.Log("Opened socket " +localSocketId);
	}

	private static void MakeInstance () {
		GameObject ob = new GameObject();
		ob.name = "Backstab";
		instance =  ob.AddComponent<Backstab>();
	}

	private static void Listen () {
		System.Object packet;
		int recSocketId;
		int recConnectionId;
		int recChannelId;
		byte[] buffer = new byte[1024];
		int recievedSize;
		byte error;
		NetworkEventType rEvent =  NetworkTransport.Receive(out recSocketId, out recConnectionId, out recChannelId, buffer, buffer.Length, out recievedSize, out error);

		switch (rEvent) {
			case NetworkEventType.Nothing:
				break;
			case NetworkEventType.ConnectEvent:
				if (isServer) {
					FindObjectOfType<NetScriptTest>().OnClientConnected();
					
					clientSocketIds[numConnections] = recSocketId;
					numConnections++;
				} else {
					serverConnectionId = recConnectionId;
					foreach (NetScript inst in NetScript.instances) {
						inst.OnConnected();
					}
				}
				break;
			case NetworkEventType.DataEvent:
				Stream stream = new MemoryStream(buffer);
				BinaryFormatter formatter = new BinaryFormatter();
				packet = formatter.Deserialize(stream);
				
				if (packet is Message) {
					GetMessage(packet as Message);
				} else if (packet is RpcData) {
					GetRpc(packet as RpcData);
				}
				break;
			case NetworkEventType.DisconnectEvent:
				foreach (NetScript inst in NetScript.instances) {
					inst.OnDisconnected();
				}
				break;
		}
	}

	static void GetMessage (Message message) {
		foreach (NetScript inst in NetScript.instances) {
			if (inst.ViewId == message.sceneId) {
				inst.OnGotMessage(message.message);
			}
		}
	}

	static void GetRpc (RpcData rpc) {
		NetScript inst = NetScript.instances[rpc.sceneId];
		inst.RecieveRpc(rpc);
	}

	//
	//Non-static
	//

	void Update () {
		Listen();
	}
}
