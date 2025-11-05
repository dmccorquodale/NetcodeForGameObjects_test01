using UnityEngine;
using Unity.Netcode;

public class CharColourPicker : NetworkBehaviour
{
    public NetworkVariable<Color> DuckColor = new NetworkVariable<Color>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public Renderer myRenderer;

    public override void OnNetworkSpawn()
    {
        DuckColor.OnValueChanged += OnColorChanged;

        if (IsOwner)
        {
            DuckColor.Value = PickMyColor();
        }
    }

    public override void OnDestroy()
    {
        DuckColor.OnValueChanged -= OnColorChanged;
    }

    private void OnColorChanged(Color oldValue, Color newValue)
    {
        myRenderer.material.color = newValue;
    }

    Color PickMyColor()
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
