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
using System.Collections.Generic;
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

public class ConnectionData {
	public string address = "127.0.0.1";
	public int port;

	public ConnectionData (string address, int port) {
		this.address = address;
		this.port = port;
	}

	public override string ToString () {
		return "IP: " +address + " Port: " + port;
	}
}

public class Backstab : MonoBehaviour {
	private static Backstab instance;
	public static bool IsActive { get { return (instance != null); } }

	public static int port = 8888;
	public static int maxConnections = 10;
	public static int packetSize = 1024;

	public static int broadcastKey = 1000;
	public static int broadcastVersion = 1;
	public static int broadcastSubVersion = 0;
	public static string broadcastMessage = "Hello!";
	public static List<ConnectionData> broadcasters = new List<ConnectionData>();

	public static int localSocketId = 0; //The socket id of this computer. You can have multiple sockets open with NetworkTransport, but Backstab only uses one.
	public static int LocalSocketId { get { return localSocketId; } }
	public static int serverConnectionId;
	public static int[] clientConnectionIds;
	public static int numConnections = 0;
	private static int reliableChannelId;
	private static int unreliableChannelId;

	private static bool isServer;
	public static bool IsServer { get { return isServer; } }
	private static bool isClient;
	public static bool IsClient { get { return isClient; } }
	public static bool IsConnected { get { return numConnections > 0; }  }

	public static int recSocketId;
	public static int recConnectionId;
	public static int recChannelId;
	public static int recievedSize;
	public static byte recError;
	
	//Basic functions

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
		if (!isServer && !isClient) {
			isServer = true;
			OpenSocket(0);

			byte error;
			NetworkTransport.StartBroadcastDiscovery(localSocketId, port, broadcastKey, broadcastVersion, broadcastSubVersion, Serialize(broadcastMessage), packetSize, 1000, out error);
			if (error != (byte)NetworkError.Ok) Debug.LogError("Failed to start broadcast discovery.");
		} else {
			Debug.LogError("Can't become a server if already server or client.");
		}
	}

	public static void StartClient () {
		if (!isServer && !isClient) {
			OpenSocket(port);
			isClient = true;

			byte error;
			NetworkTransport.SetBroadcastCredentials(localSocketId, broadcastKey, broadcastVersion, broadcastSubVersion, out error);
			if (error != (byte)NetworkError.Ok) Debug.LogError("Failed to set broadcast credentials.");
		}
	}

	//Warning: Minor Black Magic. I do not know what all of these arguments do.
	public static void Connect (string ip) {
		if (isClient && !IsConnected) {
			byte error;
			NetworkTransport.Connect(localSocketId, ip, port, 0, out error);
		} else {
			Debug.LogError("Can't connect if not a client or already connected.");
		}
	}

	//Warning: Minor Black Magic. I do not know what all of these arguments do.
	public static void Connect (string ip, int connectPort) {
		if (isClient && !IsConnected) {
			byte error;
			NetworkTransport.Connect(localSocketId, ip, connectPort, 0, out error);
		} else {
			Debug.LogError("Can't connect if not a client or already connected.");
		}
	}

	public static void Disconnect () {
		byte error;
		if (isServer) {
			for (int i = 0; i <= numConnections; i++) {
				NetworkTransport.Disconnect(localSocketId, i, out error);
			}
		}
		if (isClient) {
			NetworkTransport.Disconnect(localSocketId, serverConnectionId, out error);
		}
		numConnections = 0;
	}
	
	//HAAXXX!
	//Change this when Unity fixes RemoveHost
	public static void StopServer () {
		Disconnect();
		NetworkTransport.Shutdown();
		NetworkTransport.Init();
		isServer = false;
		isClient = false;
	}

	public static void Kick (int index) {
		byte error;
		//NetworkTransport.Disconnect(localSocketId, clientConnectionIds[index], out error);
		NetworkTransport.Disconnect(localSocketId, index + 1, out error);
	}
	
	//Sending

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
	
	//Recieving

	static void GetRpc (RpcData rpc) {
		NetScript inst = NetScript.instances[rpc.sceneId];
		inst.RecieveRpc(rpc);
	}
	
	//Private functions

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
		byte[] buffer = new byte[packetSize];
		NetworkEventType rEvent = NetworkEventType.DataEvent;

		while (rEvent != NetworkEventType.Nothing) {
			
			rEvent = NetworkTransport.Receive(out recSocketId, out recConnectionId, out recChannelId, buffer, buffer.Length, out recievedSize, out recError);
			
			switch (rEvent) {
				case NetworkEventType.Nothing:
					break;
				case NetworkEventType.ConnectEvent:
					if (isServer) {					
						clientConnectionIds[numConnections] = recSocketId;
						foreach (NetScript inst in NetScript.instances) {
							inst.OnClientConnected();
						}
					} else {
						serverConnectionId = recConnectionId;
						foreach (NetScript inst in NetScript.instances) {
							inst.OnConnectedToServer();
						}
					}
					numConnections++;
					break;
				case NetworkEventType.DataEvent:
					System.Object packet;
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
					} else {
						foreach (NetScript inst in NetScript.instances) {
							inst.OnDisconnectedFromServer();
						}
					}
					numConnections--;
					break;
				case NetworkEventType.BroadcastEvent:
					string address;
					int port;
					byte error;
					NetworkTransport.GetBroadcastConnectionInfo(localSocketId, out address, out port, out error);
					if (error != (byte)NetworkError.Ok) Debug.Log("Recieved broadcast from bad connection.");
					TryAddBroadcaster(address, port);
					foreach (NetScript inst in NetScript.instances) {
						inst.OnGotBroadcast();
					}
					break;
				default:
					Debug.Log("Unrecognized event type");
					break;
			}
		}
	}

	private static void TryAddBroadcaster (string ip, int port) {
		foreach (ConnectionData b in broadcasters) { if (b.address == ip && b.port == port) { return; } }
		broadcasters.Add(new ConnectionData(ip, port));
	}

	static bool IsConnectionOk (int i) {
		string address;
		int port;
		UnityEngine.Networking.Types.NetworkID netId;
		UnityEngine.Networking.Types.NodeID nodeId;
		byte error;
		NetworkTransport.GetConnectionInfo(localSocketId, i, out address, out port, out netId, out nodeId, out error);
		return error == (byte)NetworkError.Ok;
	}
	
	//Non-static

	void Update () {
		Listen();
	}
}
