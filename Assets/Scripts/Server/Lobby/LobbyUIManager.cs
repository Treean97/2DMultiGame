using System;
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

    [Header("Panels")]
    [SerializeField] private GameObject _LobbyPanel;
    [SerializeField] private GameObject _RoomPanel;

    [Header("Create Room UI")]
    [SerializeField] private Button _CreateRoomButton;
    [SerializeField] private GameObject _CreateRoomUI;
    [SerializeField] private TMP_InputField _RoomNameInput;
    [SerializeField] private Button _CreateRoomConfirmButton;

    [Header("List UI")]
    [SerializeField] private Button _RefreshButton;
    [SerializeField] private Transform _ListContent;
    [SerializeField] private LobbyRoomItem _RoomItemPrefab;

    [Header("Status")]
    [SerializeField] private TMP_Text _StatusText;

    [Header("Options")]
    [SerializeField] private int _MaxPlayers = 8;
    [SerializeField] private bool _IsPublic = true;

    readonly List<LobbyRoomItem> _Spawned = new();

    void Awake()
    {
        if (_CreateRoomConfirmButton != null) _CreateRoomConfirmButton.onClick.AddListener(() => _ = OnClickCreate());
        if (_RefreshButton != null) _RefreshButton.onClick.AddListener(() => _ = RefreshList());
        if (_CreateRoomButton != null) _CreateRoomButton.onClick.AddListener(ShowCreateRoomUI);
    }

    async void OnEnable()
    {
        if (_AuthManager != null) _AuthManager.OnSignedIn += HandleSignedIn;

        if (_LobbyManager != null)
        {
            _LobbyManager.OnLobbyEntered += HandleLobbyEntered;
            _LobbyManager.OnLobbyLeft += HandleLobbyLeft;
            _LobbyManager.OnLobbyListUpdated += HandleLobbyListUpdated;
        }

        try
        {
            await UgsBootstrap.EnsureInitAsync();
        }
        catch (Exception e)
        {
            HideAllUI();
            SetStatus($"UGS init failed: {e.Message}");
            return;
        }

        if (_AuthManager != null && _AuthManager.IsSignedIn) ShowLobbyUI();
        else HideAllUI();
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
        ShowLobbyUI();
    }

    void HandleLobbyEntered(Lobby lobby)
    {
        ShowRoomUI();
        SetStatus($"Entered: {lobby?.Name}");
    }

    void HandleLobbyLeft()
    {
        ShowLobbyUI();
        SetStatus("Left lobby.");
    }

    void HandleLobbyListUpdated(List<Lobby> lobbies)
    {
        RebuildList(lobbies);
        SetStatus("Lobby list updated.");
        SetInteractable(true);
    }

    void HideAllUI()
    {
        if (_LobbyPanel != null) _LobbyPanel.SetActive(false);
        if (_RoomPanel != null) _RoomPanel.SetActive(false);

        // 로비 패널이 꺼져도 Create UI 오브젝트가 씬에 남아있을 수 있으니 명시적으로 끔
        if (_RoomNameInput != null) _RoomNameInput.gameObject.SetActive(false);
        if (_CreateRoomConfirmButton != null) _CreateRoomConfirmButton.gameObject.SetActive(false);
        if (_CreateRoomUI != null) _CreateRoomUI.SetActive(false);
    }

    void ShowLobbyUI()
    {
        if (_LobbyPanel != null) _LobbyPanel.SetActive(true);
        if (_RoomPanel != null) _RoomPanel.SetActive(false);

        // 로비 목록 화면 기본 상태
        if (_RoomNameInput != null) _RoomNameInput.gameObject.SetActive(false);
        if (_CreateRoomConfirmButton != null) _CreateRoomConfirmButton.gameObject.SetActive(false);
        if (_CreateRoomUI != null) _CreateRoomUI.SetActive(false);

        _ = RefreshList();
    }

    void ShowRoomUI()
    {
        if (_LobbyPanel != null) _LobbyPanel.SetActive(false);
        if (_RoomPanel != null) _RoomPanel.SetActive(true);
    }

    // _CreateRoomButton 클릭 시 호출: Create UI 표시
    void ShowCreateRoomUI()
    {
        if (!IsReady()) return;

        if (_CreateRoomUI != null)
        {
            _CreateRoomUI.SetActive(true);
        }

        if (_RoomNameInput != null)
        {
            _RoomNameInput.gameObject.SetActive(true);
            _RoomNameInput.Select();
            _RoomNameInput.ActivateInputField();
        }

        if (_CreateRoomConfirmButton != null)
            _CreateRoomConfirmButton.gameObject.SetActive(true);

        SetStatus("Create room UI opened.");
    }

    // 방 생성
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

        try
        {
            await _LobbyManager.QueryAndNotifyAsync(20);
        }
        finally
        {
            SetInteractable(true);
        }
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

        try
        {
            bool ok = await _LobbyManager.JoinByIdAsync(lobby.Id);
            if (!ok)
            {
                SetStatus("Join failed.");
                return;
            }
        }
        finally
        {
            SetInteractable(true);
        }
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
        if (_CreateRoomButton != null) _CreateRoomButton.interactable = interactable;
        if (_CreateRoomConfirmButton != null) _CreateRoomConfirmButton.interactable = interactable;
        if (_RefreshButton != null) _RefreshButton.interactable = interactable;
        if (_RoomNameInput != null) _RoomNameInput.interactable = interactable;
    }

    void SetStatus(string msg)
    {
        if (_StatusText != null) _StatusText.text = msg;
        Debug.Log($"[LobbyUI] {msg}");
    }
}
