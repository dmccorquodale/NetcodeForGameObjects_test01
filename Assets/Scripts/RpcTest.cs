using Unity.Netcode;
using UnityEngine;

public class RpcTest : NetworkBehaviour
{
    //public Renderer myrenderer;

    public override void OnNetworkSpawn()
    {
        //Debug.Log("Am I Server: " + IsServer + "\nAm I Client: " + IsClient + "\nAm I Owner (of this gameobject?): " + IsOwner);

        if (!IsServer && IsOwner) //Only send an RPC to the server from the client that owns the NetworkObject of this NetworkBehaviour instance
        {
            //ServerOnlyRpc(0, NetworkObjectId);
        }
    }

    void Update()
    {
        ClientAndHostRpc(1, NetworkObjectId);
    }

    [Rpc(SendTo.ClientsAndHost)]
    void ClientAndHostRpc(int value, ulong sourceNetworkObjectId)
    {
        //Debug.Log($"Client Received the RPC #{value} on NetworkObject #{sourceNetworkObjectId}");
        if (IsOwner) //Only send an RPC to the owner of the NetworkObject
        {
            //Debug.Log("I am the owner of this object and my color is " + myrenderer.material.color + " and networkID is " + NetworkObjectId);
            //ServerOnlyRpc(value + 1, sourceNetworkObjectId);
        }
    }
    /*
    [Rpc(SendTo.Server)]
    void ServerOnlyRpc(int value, ulong sourceNetworkObjectId)
    {
        //Debug.Log($"Server Received the RPC #{value} on NetworkObject #{sourceNetworkObjectId}");
        ClientAndHostRpc(value, sourceNetworkObjectId);
    }*/
}
