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

    public void ChangeColor(Renderer myRenderer, Color myColor)
    {
        ChangeColorRpc(myRenderer, myColor);
        
    }

    [Rpc(SendTo.ClientsAndHost)]
    void ChangeColorRpc(Renderer myRenderer, Color myColor)
    {
        myRenderer.material.color = myColor;
    }*/
}
