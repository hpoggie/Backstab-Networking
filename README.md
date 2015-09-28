# Backstab-Networking
Networking system for Unity.

Can't figure out UNET? Wish it would do what you wanted? Try Backstab.

# How to use
To get started, open Beginning.unity. This scene is a chat demo with everything set up.

The script Backstab is the system's equivalent of NetworkManager. It needs to be on an object in your scene. It has many settings that you can play around with. See the comments in the script itself for details on those. The default settings should work fine for a test.

If you have some value that you want to synchronize across the network, put it in a script that inherits from NetScript. You can then use RPCs to synchronize it. In order to send an RPC, you must call RegisterRpc(function's name) before using it (preferably in Start or Awake). You can then call it by using the RPC functions in NetScript. There's a NetRigidbody component provided that synchronizes rigidbodies for you.
