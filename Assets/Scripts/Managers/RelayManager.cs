using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;

using TMPro;

using UnityEngine;

public class RelayManager : MonoBehaviour
{
    public TMP_Text _joinCodeText;
    public TMP_InputField _joinInput;
    public GameObject _buttons;

    private UnityTransport _transport;
    private NetworkManager _networkManager;
    private int MaxPlayers = 5;

    public async void Awake()
    {
        Debug.Log("Relay Manager Awake");
        _transport = FindAnyObjectByType<UnityTransport>();
        _networkManager = FindAnyObjectByType<NetworkManager>();
        _buttons.SetActive(false);

        await Authenticate();

        _buttons.SetActive(true);

    }

    private static async Task Authenticate()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void CreateGame()
    {
        Debug.Log("Create Game");
        _buttons.SetActive(false);
        Allocation a = await RelayService.Instance.CreateAllocationAsync(MaxPlayers);

        _joinCodeText.text = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);

        _transport.SetHostRelayData(
            a.RelayServer.IpV4,
            (ushort)a.RelayServer.Port,
            a.AllocationIdBytes,
            a.Key,
            a.ConnectionData
        );
        
        _networkManager.StartHost();
    }

    public async void JoinGame()
    {
        Debug.Log("Join Game");
        _buttons.SetActive(false);

        // Client uses JoinAllocation
        JoinAllocation a = await RelayService.Instance.JoinAllocationAsync(_joinInput.text.Trim());

        _transport.SetClientRelayData(
            a.RelayServer.IpV4,
            (ushort)a.RelayServer.Port,
            a.AllocationIdBytes,   // allocationId
            a.Key,                 // key (64 bytes)
            a.ConnectionData,      // this client's connectionData
            a.HostConnectionData,  // host's connectionData
            false                  // isSecure
        );

        _networkManager.StartClient();
    }
}

