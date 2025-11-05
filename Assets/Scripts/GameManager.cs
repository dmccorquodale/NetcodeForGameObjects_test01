using UnityEngine;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }/*

    public override void OnNetworkSpawn()
    {
        Debug.Log("My GameManager Spawned - time to sync colours");

        SendMeYourColour();
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    void SendMeYourColour()
    {

    }*/
}
