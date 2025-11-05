using UnityEngine;

public class CharColourPicker : NetworkBehaviour
{
    public NetworkVariable<Color> PlayerColor = new NetworkVariable<Color>();

    public renderer myRenderer;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
