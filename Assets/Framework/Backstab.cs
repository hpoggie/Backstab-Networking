/*
 * How to use
 * 
 * DO NOT ASSIGN THIS SCRIPT TO A GAMEOBJECT. CALL THE STATIC FUNCTIONS.
 * Before doing anything, call Backstab.Init().
 * To start a server, call Backstab.StartServer().
 * To connect to a server, call Backstab.Connect(someIp), where someIp is the ip address of the server.
 * To disconnect or stop the server, call Backstab.Disconnect().
 * To quit, call Backstab.Quit().
 * 
 * Backstab has no SyncVars. Everything must be done from RPCs.
 * RPCs must be done from NetScript components; all your scripts with RPCs must inherit from NetScript.
 * 
 * Backstab uses UDP, not Websocket.
 * 
 * Backstab contains Heavy Wizardry and a little Black Magic. Modify this code at your own risk.
 * If you do choose to modify it, do not expect the docs to be helpful.
 */

using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[System.Serializable]
public class RpcData {
	public int sceneId;
	public byte methodId;
	public System.Object[] args;

	public RpcData (int sceneId, byte methodId, System.Object[] args) {
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
	public static uint packetSize = 1024;

	public static int localSocketId = 0; //The socket id of this computer. You can have multiple sockets open with NetworkTransport, but Backstab only uses one.
	public static int LocalSocketId { get { return localSocketId; } }
	public static int serverConnectionId;
	public static int[] clientConnectionIds;
	public static int numConnections = 0;
	public static bool IsConnected { get { return isServer || isClient; }  }
	private static int reliableChannelId;
	private static int unreliableChannelId;

	private static bool isServer;
	public static bool IsServer { get { return isServer; } }
	private static bool isClient;
	public static bool IsClient { get { return isClient; } }

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

	//Warning: Minor Black Magic. I do not know what all of these arguments do.
	public static void Connect (string ip) {
		OpenSocket(0);
		isClient = true;
		byte error;
		NetworkTransport.Connect(localSocketId, ip, port, 0, out error);
	}

	public static void Disconnect () {
		byte error;
		if (isServer) {
			NetworkTransport.RemoveHost(localSocketId);
			isServer = false;
		}
		if (isClient) {
			NetworkTransport.Disconnect(localSocketId, serverConnectionId, out error);
			isClient = false;
		}
	}

	public static void Kick (int index) {
		byte error;
		//NetworkTransport.Disconnect(localSocketId, clientConnectionIds[index], out error);
		NetworkTransport.Disconnect(localSocketId, index + 1, out error);
	}

	//
	//Sending
	//

	public static void RpcAllReliable (int viewId, byte methodId, System.Object[] args) {
		RpcAll(viewId, methodId, reliableChannelId, args);
	}

	public static void RpcAllUnreliable (int viewId, byte methodId, System.Object[] args) {
		RpcAll(viewId, methodId, unreliableChannelId, args);
	}

	public static void RpcAll (int viewId, byte methodId, int channelId, System.Object[] args) {
		if (isServer) {
			for (int i = 0; i < Backstab.clientConnectionIds.Length; i++) {
				if (IsConnectionOk(i)) {
					Rpc(viewId, methodId, args, channelId, i);
				}
			}
		} else {
			Debug.LogError("Not the server. Can't send to clients.");
		}
	}

	public static void RpcReliable (int viewId, byte methodId, System.Object[] args, int connectionId) {
		Rpc(viewId, methodId, args, reliableChannelId, connectionId);
	}

	public static void RpcUnreliable (int viewId, byte methodId, System.Object[] args, int connectionId) {
		Rpc(viewId, methodId, args, unreliableChannelId, connectionId);
	}

	public static void Rpc (int viewId, byte methodId, System.Object[] args, int channelId, int connectionId) {
		Send(new RpcData(viewId, methodId, args), channelId, connectionId);
	}

	public static void Send (System.Object packet, int channelId, int targetId) {
		byte error;
		byte[] buffer = Serialize(packet);
		NetworkTransport.Send(localSocketId, targetId, channelId, buffer, buffer.Length, out error);
	}

	private static byte[] Serialize (System.Object packet) {
		byte[] buffer = new byte[packetSize];
		new BinaryFormatter().Serialize(new MemoryStream(buffer), packet);
		return buffer;
	}

	//
	//Recieving
	//

	static void GetRpc (RpcData rpc) {
		NetScript inst = NetScript.instances[rpc.sceneId];
		inst.RecieveRpc(rpc);
	}

	//
	//Private functions
	//

	private static void OpenSocket (int socketPort) {
		ConnectionConfig config = new ConnectionConfig();
		reliableChannelId = config.AddChannel(QosType.Reliable);
		unreliableChannelId = config.AddChannel(QosType.Unreliable);
		ConnectionConfig.Validate(config);

		clientConnectionIds = new int[maxConnections];
		HostTopology topology = new HostTopology(config, maxConnections);
		localSocketId = NetworkTransport.AddHost(topology, socketPort, null);
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
		byte[] buffer = new byte[packetSize];
		int recievedSize;
		byte error;
		NetworkEventType rEvent =  NetworkTransport.Receive(out recSocketId, out recConnectionId, out recChannelId, buffer, buffer.Length, out recievedSize, out error);

		switch (rEvent) {
			case NetworkEventType.Nothing:
				break;
			case NetworkEventType.ConnectEvent:
				if (isServer) {					
					clientConnectionIds[numConnections] = recSocketId;
					numConnections++;
					foreach (NetScript inst in NetScript.instances) {
						inst.OnClientConnected();
					}
				} else {
					serverConnectionId = recConnectionId;
					foreach (NetScript inst in NetScript.instances) {
						inst.OnConnectedToServer();
					}
				}
				break;
			case NetworkEventType.DataEvent:
				Stream stream = new MemoryStream(buffer);
				BinaryFormatter formatter = new BinaryFormatter();
				packet = formatter.Deserialize(stream);
				
				if (packet is RpcData) {
					GetRpc(packet as RpcData);
				}
				break;
			case NetworkEventType.DisconnectEvent:
				if (isServer) {
					foreach (NetScript inst in NetScript.instances) {
						inst.OnClientDisconnected();
					}
					numConnections--;
				} else {
					foreach (NetScript inst in NetScript.instances) {
						inst.OnDisconnectedFromServer();
					}
					NetworkTransport.RemoveHost(localSocketId);
					isClient = false;
				}
				break;
		}
	}

	static bool IsConnectionOk (int i) {
		string address;
		int port;
		UnityEngine.Networking.Types.NetworkID netId;
		UnityEngine.Networking.Types.NodeID nodeId;
		byte error;
		NetworkTransport.GetConnectionInfo(Backstab.localSocketId, i, out address, out port, out netId, out nodeId, out error);
		return error == (byte)NetworkError.Ok;
	}

	//
	//Non-static
	//

	void Update () {
		Listen();
	}
}
