# Backstab-Networking
Networking system for the Unity game engine.

Can't figure out UNET? Wish it would do what you wanted? Try Backstab.

# How to use
To get started, open Beginning.unity. This scene is a chat demo with everything set up.

The script Backstab is the system's equivalent of NetworkManager. It needs to be on an object in your scene. It has many settings that you can play around with. See the comments in the script itself for details on those. The default settings should work fine for a test.

To start a server, call backstab.StartServer().

To start a client, call backstab.StartClient().

For the sake of clarity, yes, that's a lowercase "b" at the start of "backstab." NetScripts have an instance variable that points to the first instance of Backstab found on start.

# Remote Procedure Calls
If you have some value that you want to synchronize across the network, put it in a script that inherits from NetScript. You can then use RPCs to synchronize it.

In order to send an RPC, it must have an Rpc attribute. There are two kinds of Rpc attribute: Server and Client. Server means it's sent from a client to the server, while Client means it's sent from the server to all clients.

Like so:

```c#
[RpcServer]
public void MyFunction (int foo) {
  Debug.Log(foo); //Sends an integer message to the server
}
```

To actually send an RPC, you would do something like `Rpc("MyFunction", args)`.

There's a NetRigidbody component provided that synchronizes rigidbodies for you.

It is important to note that RPCs can't be overloaded.

# Broadcast Discovery
When you start a server, Backstab will start broadcasting on your LAN. Any clients will be able to see this and connect to the address that's broadcasting. This means you don't have to know the target's IP address to connect over LAN.

Backstab.broadcasters holds a list of all servers from whom broadcasts were recieved.

NOTE: Backstab's broadcast discovery is not based on UNET, because UNET's discovery doesn't work on Linux for some reason.

# Future Updates
I may add the ability to overload RPCs in the future. The problem is that NetScript would have to match the argument list at runtime, which is kind of slow. I will do a load test and see whether it's a big performance draw.
