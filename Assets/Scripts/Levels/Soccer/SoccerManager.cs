using UnityEngine;
using Unity.Netcode;

public class SoccerManager : NetworkBehaviour
{
    public static SoccerManager Instance { get; private set; }

    //public int hostScore;
    public int clientScore;

    public NetworkVariable<int> hostScore = new NetworkVariable<int>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void OnEnable()
    {
        Goal.GoalScored += GoalScored;
    }

    private void OnDisable()
    {
        Goal.GoalScored -= GoalScored;
    }

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
    }

    void GoalScored()
    {
        if (!IsServer) return;

        Debug.Log("game manager witnesses a goal");

        hostScore.Value++;
    }
}
