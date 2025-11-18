using UnityEngine;
using Unity.Netcode;

public class CharColourPicker : NetworkBehaviour
{
    public NetworkVariable<Color> DuckColor = new NetworkVariable<Color>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public Renderer myRenderer;

    public override void OnNetworkSpawn()
    {
        DuckColor.OnValueChanged += OnColourChanged;

        if (IsOwner)
        {
            SetColour(ClientHostColour());
            //Debug.Log("ID OnNetworkSpawn() (IsOwner): " + NetworkObjectId);
        }

        //Debug.Log("ID OnNetworkSpawn(): " + NetworkObjectId);

        myRenderer.material.color = DuckColor.Value; // this is to sync all the character instances of late joining clients
    }

    public override void OnDestroy()
    {
        DuckColor.OnValueChanged -= OnColourChanged;
    }

    private void OnColourChanged(Color oldValue, Color newValue)
    {
        //Debug.Log("ID OnColourChanged(): " + NetworkObjectId);
        myRenderer.material.color = newValue;
    }

    public void SetColour(Color mycolour)
    {
        if (IsOwner)
            DuckColor.Value = mycolour;
    }

    Color ClientHostColour()
    {
        Color myColour = Color.green; // Don't think we should ever see a green duck

        if (IsOwner && !IsHost) // I am the client, not the host
        {
            myColour = new Color(0f, 0.66f, 1f, 1f); // blue
        }
        else // I am the host
        {
            myColour = Color.red;
        }

        return myColour;
    }
}
