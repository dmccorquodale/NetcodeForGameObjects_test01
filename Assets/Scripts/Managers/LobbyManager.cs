using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models; // for Allocation, JoinAllocation, AllocationUtils

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    public event Action<List<Lobby>> OnLobbiesUpdated;
    public event Action<string> OnStatusChanged;

    public bool IsInitialized { get; private set; } 

    Lobby _currentLobby;
    float _heartbeatTimer;
    const float HeartbeatInterval = 15f; // seconds
    LobbyUI _lobbyUI;

    async void Awake()
    {
        // Simple singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        await InitializeServicesAsync();
    }

    void Start()
    {
        // FindLobbyUI after Awake finishes
        //_lobbyUI = FindObjectOfType<LobbyUI>(true); // include inactive objects
        _lobbyUI = FindFirstObjectByType<LobbyUI>(FindObjectsInactive.Include); // unity was complaining
    }

    async Task InitializeServicesAsync()
    {
        try
        {
            OnStatusChanged?.Invoke("Initializing Unity Services...");

            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                OnStatusChanged?.Invoke("Signing in anonymously...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            IsInitialized = true; // <--- mark ready

            OnStatusChanged?.Invoke($"Signed in as: {AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            OnStatusChanged?.Invoke("Failed to initialize services: " + e.Message);
        }
    }


    void Update()
    {
        // Keep lobby alive with heartbeat
        if (_currentLobby != null)
        {
            _heartbeatTimer -= Time.deltaTime;
            if (_heartbeatTimer <= 0f)
            {
                _heartbeatTimer = HeartbeatInterval;
                _ = LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
            }
        }
    }

    // ---------- LOBBY LIST ----------

    public async Task RefreshLobbiesAsync()
    {
        if (!IsInitialized || !AuthenticationService.Instance.IsSignedIn)
        {
            OnStatusChanged?.Invoke("Lobby not ready yet (auth still in progress).");
            return;
        }

        try
        {
            OnStatusChanged?.Invoke("Refreshing lobbies...");

            var options = new QueryLobbiesOptions
            {
                Count = 10,
                Filters = new List<QueryFilter>
                {
                    // Only lobbies with at least 1 free slot
                    new QueryFilter(
                        field: QueryFilter.FieldOptions.AvailableSlots,
                        op: QueryFilter.OpOptions.GT,
                        value: "0")
                }
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);
            OnLobbiesUpdated?.Invoke(response.Results);
            OnStatusChanged?.Invoke($"Found {response.Results.Count} lobbies.");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            OnStatusChanged?.Invoke("Failed to query lobbies: " + e.Message);
            OnLobbiesUpdated?.Invoke(new List<Lobby>());
        }
    }

    // ---------- CREATE LOBBY (HOST) ----------

    public async Task CreateLobbyAsync(string lobbyName, int maxPlayers)
    {
        try
        {
            OnStatusChanged?.Invoke("Creating Relay allocation...");

            int maxConnections = maxPlayers - 1; // Relay wants connections excluding host
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);

            string relayJoinCode =
                await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            OnStatusChanged?.Invoke("Creating lobby...");

            var data = new Dictionary<string, DataObject>
            {
                // Save the relay join code into lobby data so joiners can use it
                { "joinCode", new DataObject(DataObject.VisibilityOptions.Public, relayJoinCode) }
            };

            var options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = new Player(id: AuthenticationService.Instance.PlayerId),
                Data = data
            };

            _currentLobby = await LobbyService.Instance.CreateLobbyAsync(
                lobbyName, maxPlayers, options);

            _heartbeatTimer = HeartbeatInterval;

            // Configure Relay for host and start host
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(allocation, "dtls")); // or "udp" :contentReference[oaicite:1]{index=1}

            bool started = NetworkManager.Singleton.StartHost();
            OnStatusChanged?.Invoke(
                started
                    ? $"Lobby '{_currentLobby.Name}' created. Code: {_currentLobby.LobbyCode}"
                    : "Failed to start host.");
            if (started)
            {
                // Hide UI
                //var ui = FindObjectOfType<LobbyUI>(true);
                var ui = FindFirstObjectByType<LobbyUI>(FindObjectsInactive.Include);
                ui?.HideLobbyUI();
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            OnStatusChanged?.Invoke("Failed to create lobby: " + e.Message);
        }
    }

    // ---------- JOIN LOBBY (CLIENT) ----------

    public async Task JoinLobbyAsync(Lobby lobby)
    {
        try
        {
            OnStatusChanged?.Invoke($"Joining lobby '{lobby.Name}'...");

            _currentLobby = lobby;
            _heartbeatTimer = HeartbeatInterval;

            // Read Relay join code from lobby data
            if (!lobby.Data.TryGetValue("joinCode", out var joinCodeObj))
            {
                OnStatusChanged?.Invoke("Lobby has no Relay join code.");
                return;
            }

            string relayJoinCode = joinCodeObj.Value;
            OnStatusChanged?.Invoke("Joining Relay allocation...");

            JoinAllocation allocation =
                await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(allocation, "dtls"));

            bool started = NetworkManager.Singleton.StartClient();
            OnStatusChanged?.Invoke(
                started
                    ? "Connecting to host..."
                    : "Failed to start client.");
            if (started)
            {
                // Hide UI
                //var ui = FindObjectOfType<LobbyUI>(true);
                var ui = FindFirstObjectByType<LobbyUI>(FindObjectsInactive.Include);
                
                ui?.HideLobbyUI();
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            OnStatusChanged?.Invoke("Failed to join lobby: " + e.Message);
        }
    }
}
