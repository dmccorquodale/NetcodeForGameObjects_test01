using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void HostGameClicked()
    {
        SceneManager.LoadScene("HubWorld");
    }
}
