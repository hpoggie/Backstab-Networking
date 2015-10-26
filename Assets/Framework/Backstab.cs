/*
 * How to use
 * 
 * To start a server, call StartServer().
 * To start a client, call StartClient().
 * To connect to a server, call Connect(someIp), where someIp is the ip address of the server.
 * To disconnect or stop the server, call Disconnect().
 * To quit, call Quit().
 * 
 * Backstab has no SyncVars. Everything must be done from RPCs.
 * RPCs must be done from NetScript components; all your scripts with RPCs must inherit from NetScript.
 * 
 * Backstab uses UDP, not Websocket.
 * 
 * 
 * Settings
 *
 * Port: the port to connect over.
 * Max Connections: maximum number of connections we can have (only important if running a server)
 * Packet size: the size of sent byte arrays
 * Broadcast Key, Version, & Subversion: must be the same as broadcaster to recieve broadcasts
 * Broadcast Message: data sent on successful broadcast
 *
 * NOTE
 *
 * Backstab contains Heavy Wizardry and a little Black Magic. Modify this code at your own risk.
 * If you do choose to modify it, do not expect the docs to be helpful.
 *
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
	public string message;

	public ConnectionData (string address, int port) {
		this.address = address;
		this.port = port;
	}

	public override string ToString () {
		return message + " IP: " +address + " Port: " + port;
	}
}

[System.Serializable]
public class ChannelData {
	public QosType qosType;
	[ReadOnly] public int id;
}

public class Backstab : MonoBehaviour {
	public static List<Backstab> instances = new List<Backstab>();
	public static bool IsActive { get { return (instances.Count > 0); } }

	public int port = 8888;
	public int maxConnections = 10;
	public int packetSize = 1024;

	public int broadcastKey = 1000;
	public int broadcastVersion = 1;
	public int broadcastSubVersion = 0;
	public string broadcastMessage = "Hello!";
	public List<ConnectionData> broadcasters = new List<ConnectionData>();

	private int localSocketId = 0; //The socket id of this computer. You can have multiple sockets open with NetworkTransport, but Backstab only uses one.
	public int LocalSocketId { get { return localSocketId; } }
	private int serverConnectionId;
	private int[] clientConnectionIds;
	private int numConnections = 0;
	public int NumConnections { get { return numConnections; } }

	public ChannelData[] channelData;

	private bool isServer;
	public bool IsServer { get { return isServer; } }
	private bool isClient;
	public bool IsClient { get { return isClient; } }
	public bool IsConnected { get { return numConnections > 0; }  }
	
	[ReadOnly] public int recSocketId;
	[ReadOnly] public int recConnectionId;
	[ReadOnly] public int recChannelId;
	[ReadOnly] public int recievedSize;
	[ReadOnly] public byte recError;
	
	//Basic functions

	public static void Quit () {
		NetworkTransport.Shutdown();
	}

	public void StartServer () {
		if (!isServer && !isClient) {
			isServer = true;
			OpenSocket(0);

			byte error;
			NetworkTransport.StartBroadcastDiscovery(localSocketId, port, broadcastKey, broadcastVersion, broadcastSubVersion, Serialize(broadcastMessage), packetSize, 1000, out error);
			if (error != (byte)NetworkError.Ok) Debug.LogError("Failed to start broadcast discovery.");

			foreach (NetScript inst in NetScript.instances) {
				inst.OnBackstabStartServer();
			}
		} else {
			Debug.LogError("Can't become a server if already server or client.");
		}
	}

	public void StartClient () {
		if (!isServer && !isClient) {
			OpenSocket(port);
			isClient = true;

			byte error;
			NetworkTransport.SetBroadcastCredentials(localSocketId, broadcastKey, broadcastVersion, broadcastSubVersion, out error);
			if (error != (byte)NetworkError.Ok) Debug.LogError("Failed to set broadcast credentials.");

			foreach (NetScript inst in NetScript.instances) {
				inst.OnBackstabStartClient();
			}
		}
	}

	//Warning: Minor Black Magic. I do not know what all of these arguments do.
	public void Connect (string ip) {
		Connect(ip, port);	
	}

	//Warning: Minor Black Magic. I do not know what all of these arguments do.
	public void Connect (string ip, int connectPort) {
		if (isClient && !IsConnected) {
			byte error;
			NetworkTransport.Connect(localSocketId, ip, connectPort, 0, out error);
		} else {
			Debug.LogError("Can't connect if not a client or already connected.");
		}
	}

	public void Disconnect () {
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
	public void StopServer () {
		Disconnect();
		NetworkTransport.Shutdown();
		NetworkTransport.Init();
		isServer = false;
		isClient = false;
		foreach (NetScript inst in NetScript.instances) {
			inst.OnBackstabStopServer();
		}
	}

	public void Kick (int index) {
		byte error;
		NetworkTransport.Disconnect(localSocketId, index + 1, out error);
	}
	
	//Sending

	public void RpcAll (int viewId, byte methodId, QosType qtype, object[] args) {
		RpcAll(viewId, methodId, GetChannel(qtype), args);
	}

	public void RpcAll (int viewId, byte methodId, int channelId, System.Object[] args) {
		if (isServer) {
			for (int i = 0; i < clientConnectionIds.Length; i++) {
				if (IsConnectionOk(i)) {
					Rpc(viewId, methodId, channelId, i, args);
				}
			}
		} else {
			Debug.LogError("Not the server. Can't send to clients.");
		}
	}

	public void Rpc (int viewId, byte methodId, QosType qtype, int connectionId, System.Object[] args) {
		Rpc(viewId, methodId, GetChannel(qtype), connectionId, args);
	}

	public void Rpc (int viewId, byte methodId, int channelId, int connectionId, System.Object[] args) {
		Send(new RpcData(viewId, methodId, args), channelId, connectionId);
	}

	public void Send (System.Object packet, int channelId, int targetId) {
		byte error;
		byte[] buffer = Serialize(packet);
		NetworkTransport.Send(localSocketId, targetId, channelId, buffer, buffer.Length, out error);
	}

	private byte[] Serialize (System.Object packet) {
		byte[] buffer = new byte[packetSize];
		new BinaryFormatter().Serialize(new MemoryStream(buffer), packet);
		return buffer;
	}

	private static object Deserialize (byte[] buffer) {
		Stream stream = new MemoryStream(buffer);
		BinaryFormatter formatter = new BinaryFormatter();
		return formatter.Deserialize(stream);
	}

	//Recieving

	static void GetRpc (RpcData rpc) {
		NetScript inst = NetScript.instances[rpc.sceneId];
		inst.RecieveRpc(rpc);
	}
	
	//Private functions

	private void OpenSocket (int socketPort) {
		ConnectionConfig config = new ConnectionConfig();
		foreach (ChannelData d in channelData) { d.id = config.AddChannel(d.qosType); }
		ConnectionConfig.Validate(config);

		clientConnectionIds = new int[maxConnections];
		HostTopology topology = new HostTopology(config, maxConnections);
		localSocketId = NetworkTransport.AddHost(topology, socketPort, null);
	}

	private void Listen () {
		byte[] buffer = new byte[packetSize];
		NetworkEventType rEvent = NetworkEventType.DataEvent;

		while (rEvent != NetworkEventType.Nothing) {
			
			rEvent = NetworkTransport.Receive(out recSocketId, out recConnectionId, out recChannelId, buffer, buffer.Length, out recievedSize, out recError);
			
			switch (rEvent) {
				case NetworkEventType.Nothing:
					break;
				case NetworkEventType.ConnectEvent:
					if (recError == (byte)NetworkError.Ok) {
						ConnectionData data = GetConnectionData(recConnectionId);
						numConnections++;
						if (isServer) {
							clientConnectionIds[numConnections] = recSocketId;
							foreach (NetScript inst in NetScript.instances) {
								inst.OnBackstabClientConnected(data);
							}
						} else {
							serverConnectionId = recConnectionId;
							foreach (NetScript inst in NetScript.instances) {
								inst.OnBackstabConnectedToServer(data);
							}
						}
					} else {
						foreach (NetScript inst in NetScript.instances) {
							inst.OnBackstabFailedToConnect();
						}
					}
					break;
				case NetworkEventType.DataEvent:
					System.Object packet = Deserialize(buffer);
					if (packet is RpcData) {
						GetRpc(packet as RpcData);
					}
					break;
				case NetworkEventType.DisconnectEvent:
					if (isServer) {
						foreach (NetScript inst in NetScript.instances) {
							inst.OnBackstabClientDisconnected();
						}
					} else {
						foreach (NetScript inst in NetScript.instances) {
							inst.OnBackstabDisconnectedFromServer();
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
					NetworkTransport.GetBroadcastConnectionMessage(localSocketId, buffer, buffer.Length, out recievedSize, out error);
					string message = (string)Deserialize(buffer);
					TryAddBroadcaster(address, port, message);
					foreach (NetScript inst in NetScript.instances) {
						inst.OnBackstabGotBroadcast();
					}
					break;
				default:
					Debug.Log("Unrecognized event type");
					break;
			}
		}
	}

	private void TryAddBroadcaster (string ip, int port, string message) {
		foreach (ConnectionData b in broadcasters) {
			if (b.address == ip && b.port == port) {
				b.message = message;
				return;
			}
		}
		broadcasters.Add(new ConnectionData(ip, port));
	}

	public bool IsConnectionOk (int i) {
		string address;
		int port;
		UnityEngine.Networking.Types.NetworkID netId;
		UnityEngine.Networking.Types.NodeID nodeId;
		byte error;
		NetworkTransport.GetConnectionInfo(localSocketId, i, out address, out port, out netId, out nodeId, out error);
		return error == (byte)NetworkError.Ok;
	}

	public ConnectionData GetConnectionData (int i) {
		string address;
		int port;
		UnityEngine.Networking.Types.NetworkID netId;
		UnityEngine.Networking.Types.NodeID nodeId;
		byte error;
		NetworkTransport.GetConnectionInfo(localSocketId, i, out address, out port, out netId, out nodeId, out error);
		if (error == (byte)NetworkError.Ok) {
			return new ConnectionData(address, port);
		} else {
			return null;
		}
	}

	private int GetChannel (QosType qtype) {
		for (int i = 0; i < channelData.Length; i++) {
			if (channelData[i].qosType == qtype) {
				return channelData[i].id;
			}
		}
		return -1;
	}

	//Non-static

	void Awake () {
		if (!IsActive) NetworkTransport.Init();
		instances.Add(this);
		DontDestroyOnLoad(this);
	}

	void Update () {
		Listen();
	}

	void OnDestroy () {
		Disconnect();
		isServer = false;
	}

	void OnApplicationQuit () {
		Disconnect();
	}

	//GUI hooks

	public void SetBroadcasterMessage (string message) { broadcastMessage = message; }
	public void SetMaxConnections (int max) { maxConnections = max; }

}
