using UnityEngine;
using Unity.Netcode;
using System;

public class Goal : NetworkBehaviour
{
    public static event Action GoalScored;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Ball"))
        {
            Debug.Log("GOAL!");

            GoalScored?.Invoke();
        }
    }
}
