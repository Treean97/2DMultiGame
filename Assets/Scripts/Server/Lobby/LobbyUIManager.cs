using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private AuthManager _AuthManager;
    [SerializeField] private LobbyManager _LobbyManager;

    [Header("Panels (Completely Separate)")]
    [SerializeField] private GameObject _LobbyPanel; // 로비 전용 패널(목록/생성)
    [SerializeField] private GameObject _RoomPanel;  // 룸 전용 패널(참가자/나가기)

    [Header("Create Room UI (LobbyPanel)")]
    [SerializeField] private TMP_InputField _RoomNameInput;
    [SerializeField] private Button _CreateButton;

    [Header("List UI (LobbyPanel)")]
    [SerializeField] private Button _RefreshButton;
    [SerializeField] private Transform _ListContent;
    [SerializeField] private LobbyRoomItem _RoomItemPrefab;

    [Header("Status (optional)")]
    [SerializeField] private TMP_Text _StatusText;

    [Header("Options")]
    [SerializeField] private int _MaxPlayers = 8;
    [SerializeField] private bool _IsPublic = true;

    readonly List<LobbyRoomItem> _Spawned = new();

    void Awake()
    {
        if (_CreateButton != null) _CreateButton.onClick.AddListener(() => _ = OnClickCreate());
        if (_RefreshButton != null) _RefreshButton.onClick.AddListener(() => _ = RefreshList());
    }

    void OnEnable()
    {
        if (_AuthManager != null) _AuthManager.OnSignedIn += HandleSignedIn;

        if (_LobbyManager != null)
        {
            _LobbyManager.OnLobbyEntered += HandleLobbyEntered;
            _LobbyManager.OnLobbyLeft += HandleLobbyLeft;
            _LobbyManager.OnLobbyListUpdated += HandleLobbyListUpdated;
        }

        // 시작 상태
        if (_AuthManager != null && _AuthManager.IsSignedIn) ShowLobbyPanel();
        else HideAllPanels(); // 로그인 UI는 AuthUIManager가 담당한다고 가정
    }

    void OnDisable()
    {
        if (_AuthManager != null) _AuthManager.OnSignedIn -= HandleSignedIn;

        if (_LobbyManager != null)
        {
            _LobbyManager.OnLobbyEntered -= HandleLobbyEntered;
            _LobbyManager.OnLobbyLeft -= HandleLobbyLeft;
            _LobbyManager.OnLobbyListUpdated -= HandleLobbyListUpdated;
        }
    }

    void HandleSignedIn()
    {
        ShowLobbyPanel();
    }

    void HandleLobbyEntered(Lobby lobby)
    {
        ShowRoomPanel();
        SetStatus($"Entered: {lobby?.Name}");
    }

    void HandleLobbyLeft()
    {
        ShowLobbyPanel();
        SetStatus("Left lobby.");
    }

    void HandleLobbyListUpdated(List<Lobby> lobbies)
    {
        RebuildList(lobbies);
        SetStatus("Lobby list updated.");
        SetInteractable(true);
    }

    void HideAllPanels()
    {
        if (_LobbyPanel != null) _LobbyPanel.SetActive(false);
        if (_RoomPanel != null) _RoomPanel.SetActive(false);
    }

    void ShowLobbyPanel()
    {
        if (_LobbyPanel != null) _LobbyPanel.SetActive(true);
        if (_RoomPanel != null) _RoomPanel.SetActive(false);

        _ = RefreshList();
    }

    void ShowRoomPanel()
    {
        if (_LobbyPanel != null) _LobbyPanel.SetActive(false);
        if (_RoomPanel != null) _RoomPanel.SetActive(true);
    }

    async Task OnClickCreate()
    {
        if (!IsReady()) return;

        string roomName = _RoomNameInput != null ? _RoomNameInput.text : "";
        roomName = (roomName ?? "").Trim();

        if (string.IsNullOrWhiteSpace(roomName))
        {
            SetStatus("Room name is empty.");
            return;
        }

        SetInteractable(false);
        SetStatus("Creating room...");

        bool ok = await _LobbyManager.CreateLobbyAsync(roomName, _MaxPlayers, _IsPublic);
        if (!ok)
        {
            SetStatus("Create room failed.");
            SetInteractable(true);
            return;
        }

        // 성공 전환은 OnLobbyEntered 이벤트가 처리
        SetInteractable(true);
    }

    async Task RefreshList()
    {
        if (!IsReady()) return;

        SetInteractable(false);
        SetStatus("Loading lobby list...");

        await _LobbyManager.QueryAndNotifyAsync(20);
        // 결과는 OnLobbyListUpdated 이벤트로 옴
    }

    void RebuildList(List<Lobby> lobbies)
    {
        for (int i = 0; i < _Spawned.Count; i++)
            if (_Spawned[i] != null) Destroy(_Spawned[i].gameObject);
        _Spawned.Clear();

        if (_RoomItemPrefab == null || _ListContent == null) return;
        if (lobbies == null) return;

        for (int i = 0; i < lobbies.Count; i++)
        {
            Lobby lobby = lobbies[i];
            LobbyRoomItem item = Instantiate(_RoomItemPrefab, _ListContent);
            item.Bind(lobby, OnClickJoinLobby);
            _Spawned.Add(item);
        }
    }

    async void OnClickJoinLobby(Lobby lobby)
    {
        if (lobby == null) return;
        if (!IsReady()) return;

        SetInteractable(false);
        SetStatus($"Joining... ({lobby.Name})");

        bool ok = await _LobbyManager.JoinByIdAsync(lobby.Id);
        if (!ok)
        {
            SetStatus("Join failed.");
            SetInteractable(true);
            return;
        }

        // 성공 전환은 OnLobbyEntered 이벤트가 처리
        SetInteractable(true);
    }

    bool IsReady()
    {
        if (_AuthManager == null)
        {
            Debug.LogError("[LobbyUI] AuthManager ref is null.");
            return false;
        }
        if (_LobbyManager == null)
        {
            Debug.LogError("[LobbyUI] LobbyManager ref is null.");
            return false;
        }
        if (!_AuthManager.IsSignedIn)
        {
            SetStatus("You must sign in first.");
            return false;
        }
        return true;
    }

    void SetInteractable(bool interactable)
    {
        if (_CreateButton != null) _CreateButton.interactable = interactable;
        if (_RefreshButton != null) _RefreshButton.interactable = interactable;
        if (_RoomNameInput != null) _RoomNameInput.interactable = interactable;
    }

    void SetStatus(string msg)
    {
        if (_StatusText != null) _StatusText.text = msg;
        Debug.Log($"[LobbyUI] {msg}");
    }
}
