using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;

public class Scoreboard : NetworkBehaviour
{
    public Text myText;

    public override void OnNetworkSpawn()
    {
        SoccerManager.Instance.hostScore.OnValueChanged += OnScoreChanged;

        //Debug.Log("current value: " + GameManager.Instance.hostScore.Value.ToString());
        //Debug.Log("Scoreboard spawned, updating my scoreboard");
        //SetScore(); // this is to sync, but it happens too early I guess. 

        // so this is jank, should probably be handled with an Rpc call to the server, and it returns the correct score?
        /*
        if (!IsServer)
        {
            // Wait one frame to make sure NetworkVariable has synced
            StartCoroutine(UpdateScoreNextFrame());
        }*/
        if (IsServer) { return; }

        Debug.Log("Hi I joined late, can I update the score?");
        RequestSyncScoreForLateJoinerRpc();
    }

    [Rpc(SendTo.Server)]
    void RequestSyncScoreForLateJoinerRpc()
    {
        //Debug.Log($"Server Received the RPC #{value} on NetworkObject #{sourceNetworkObjectId}");
        Debug.Log("This is the RequestSyncScoreForLateJoinerRpc() function");
        SetScoreRpc(SoccerManager.Instance.hostScore.Value);
    }

    [Rpc(SendTo.ClientsAndHost)]
    void SetScoreRpc(int score)
    {
        Debug.Log("This is the SetScoreRpc() function");
        SetScore(score.ToString());
    }

    public override void OnDestroy()
    {
        SoccerManager.Instance.hostScore.OnValueChanged -= OnScoreChanged;
    }

    void OnScoreChanged(int oldValue, int newValue)
    {
        Debug.Log("This is the OnScoreChanged() function");
        SetScore(SoccerManager.Instance.hostScore.Value.ToString());
    }

    void SetScore(string scoreToSet)
    {
        Debug.Log("This is the SetScore() function");
        myText.text = scoreToSet;
    }

    /*
    private IEnumerator UpdateScoreNextFrame()
    {
        yield return null; // wait one frame
        SetScore();
    }*/
}
