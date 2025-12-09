using UnityEngine;
using Unity.Netcode;

public class SheepSpawner : NetworkBehaviour
{
    public SheepAgent sheepPrefab;
    public int initialCount = 30;
    public Vector3 areaSize = new Vector3(20, 0, 20);
    public Vector3 center = Vector3.zero;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        for (int i = 0; i < initialCount; i++)
        {
            Vector3 pos = center + new Vector3(
                Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f),
                0f,
                Random.Range(-areaSize.z * 0.5f, areaSize.z * 0.5f)
            );

            SheepAgent sheep = Instantiate(sheepPrefab, pos, Quaternion.identity);
            sheep.GetComponent<NetworkObject>().Spawn();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, areaSize);
    }
}
