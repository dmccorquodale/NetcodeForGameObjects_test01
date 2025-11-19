using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class LevelPortal : NetworkBehaviour
{
    public string levelToLoad;

    void OnTriggerEnter()
    {
        if (IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(levelToLoad, LoadSceneMode.Single);
        }
    }
}
