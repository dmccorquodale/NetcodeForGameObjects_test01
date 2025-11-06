using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;

public class Scoreboard : NetworkBehaviour
{
    public Text myText;

    public override void OnNetworkSpawn()
    {
        GameManager.Instance.hostScore.OnValueChanged += OnScoreChanged;

        //Debug.Log("current value: " + GameManager.Instance.hostScore.Value.ToString());
        //Debug.Log("Scoreboard spawned, updating my scoreboard");
        //SetScore(); // this is to sync, but it happens too early I guess. 

        // so this is jank, should probably be handled with an Rpc call to the server, and it returns the correct score?
        if (!IsServer)
        {
            // Wait one frame to make sure NetworkVariable has synced
            StartCoroutine(UpdateScoreNextFrame());
        }
    }

    public override void OnDestroy()
    {
        GameManager.Instance.hostScore.OnValueChanged -= OnScoreChanged;
    }

    void OnScoreChanged(int oldValue, int newValue)
    {
        SetScore();
    }

    void SetScore()
    {
        myText.text = GameManager.Instance.hostScore.Value.ToString();
    }

    private IEnumerator UpdateScoreNextFrame()
    {
        yield return null; // wait one frame
        SetScore();
    }
}
