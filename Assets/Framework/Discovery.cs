using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class Discovery : MonoBehaviour {
	private static Discovery instance;

	public IPAddress groupAddress;
	public string ipAddress = "224.0.0.224";
	public int multicastPort = 8889;
	public string message;
	
	[ReadOnly] public string recServerIp;
	[ReadOnly] public string recMessage;

	private IPEndPoint remoteEnd;
	private UdpClient udpClient;

	private Backstab backstab;

	public static Discovery Instance {
		get {
			return instance;
		}
	}

	void Start () {
		groupAddress = IPAddress.Parse(ipAddress);
		instance = this;
		backstab = FindObjectOfType<Backstab>();
	}

	void ServerLookup (IAsyncResult ar) {
		byte[] recBytes = udpClient.EndReceive(ar, ref remoteEnd);
		recMessage = Encoding.ASCII.GetString(recBytes);
		recServerIp = remoteEnd.Address.ToString();
		backstab.TryAddBroadcaster(recServerIp, multicastPort,  recMessage);
	}

	public void StartBroadcast () {
		udpClient = new UdpClient();
		udpClient.JoinMulticastGroup(groupAddress);
		remoteEnd = new IPEndPoint(groupAddress, multicastPort);
		InvokeRepeating("Broadcast", 0, 1);
	}

	void Broadcast () {
		byte[] buffer = Encoding.ASCII.GetBytes(message);
		udpClient.Send(buffer, buffer.Length, remoteEnd);
	}

	public void StartListening () {
		remoteEnd = new IPEndPoint(IPAddress.Any, multicastPort);
		udpClient = new UdpClient(remoteEnd);
		udpClient.JoinMulticastGroup(groupAddress);
		udpClient.BeginReceive(new AsyncCallback(ServerLookup), null);
	}
}
