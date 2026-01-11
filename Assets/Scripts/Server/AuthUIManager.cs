using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.UI;

public class AuthUIManager : MonoBehaviour
{
    [Header("회원가입 UI")]
    [SerializeField] private TMP_InputField _SignUpUsernameInput;
    [SerializeField] private TMP_InputField _SignUpPasswordInput;
    [SerializeField] private Button _SignUpButton;

    [Header("로그인 UI")]
    [SerializeField] private TMP_InputField _SignInUsernameInput;
    [SerializeField] private TMP_InputField _SignInPasswordInput;
    [SerializeField] private Button _SignInButton;

    [Header("상태 표기")]
    [SerializeField] private TMP_Text _StatusText;

    [Header("세팅")]
    [SerializeField] private GameObject _LobbyUI;

    void Awake()
    {
        if (_SignUpButton != null) _SignUpButton.onClick.AddListener(() => _ = OnClickSignUp());
        if (_SignInButton != null) _SignInButton.onClick.AddListener(() => _ = OnClickSignIn());
        SetStatus("Ready.");
    }

    async Task OnClickSignUp()
    {
        if (AuthManager._Inst == null)
        {
            Debug.LogError("[AuthUI] AuthManager._Inst is null. 씬에 AuthManager가 있어야 합니다.");
            return;
        }

        string username = _SignUpUsernameInput != null ? _SignUpUsernameInput.text : "";
        string password = _SignUpPasswordInput != null ? _SignUpPasswordInput.text : "";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            SetStatus("SignUp: username/password is empty.");
            return;
        }

        SetInteractable(false);
        SetStatus("Signing up...");

        bool ok = false;
        try
        {
            ok = await AuthManager._Inst.SignUpAsync(username, password);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AuthUI] SignUp exception: {e}");
        }

        if (!ok)
        {
            SetStatus("SignUp failed. (check console)");
            SetInteractable(true);
            return;
        }

        // 가입 후 자동 로그인
        if (!AuthManager._Inst.IsSignedIn)
        {
            SetStatus("SignUp ok. Signing in...");
            bool signedIn = await SafeSignIn(username, password);
            if (!signedIn)
            {
                SetStatus("SignUp ok, but SignIn failed. (check console)");
                SetInteractable(true);
                return;
            }
        }

        SetStatus($"Auth ok. PlayerId={AuthManager._Inst.PlayerId}");
        await LogLobbyConnectionAsync();

        SetInteractable(true);
    }

    async Task OnClickSignIn()
    {
        if (AuthManager._Inst == null)
        {
            Debug.LogError("[AuthUI] AuthManager._Inst is null. 씬에 AuthManager가 있어야 합니다.");
            return;
        }

        string username = _SignInUsernameInput != null ? _SignInUsernameInput.text : "";
        string password = _SignInPasswordInput != null ? _SignInPasswordInput.text : "";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            SetStatus("SignIn: username/password is empty.");
            return;
        }

        SetInteractable(false);
        SetStatus("Signing in...");

        bool ok = await SafeSignIn(username, password);

        if (!ok)
        {
            SetStatus("SignIn failed. (check console)");
            SetInteractable(true);
            return;
        }

        SetStatus($"Auth ok. PlayerId={AuthManager._Inst.PlayerId}");
        bool lobbyOk = await LogLobbyConnectionAsync(); // <- bool로 바꿀 거임
        if (!lobbyOk)
        {
            SetInteractable(true);
            return;
        }

        OpenLobbyUI();
        SetInteractable(true);
    }

    void OpenLobbyUI()
    {
        _LobbyUI.SetActive(true);
    }

    async Task<bool> SafeSignIn(string username, string password)
    {
        try
        {
            return await AuthManager._Inst.SignInAsync(username, password);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AuthUI] SignIn exception: {e}");
            return false;
        }
    }

    async Task<bool> LogLobbyConnectionAsync()
    {
        try
        {
            await UgsBootstrap.EnsureInitAsync();

            var options = new QueryLobbiesOptions { Count = 1 };
            var res = await LobbyService.Instance.QueryLobbiesAsync(options);

            Debug.Log($"[AuthUI] 로비 서비스 접속 성공 (Query OK). results={res?.Results?.Count ?? 0}");
            SetStatus("Lobby connected. (Query OK)");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[AuthUI] 로비 서비스 접속 실패: {e}");
            SetStatus("Lobby connect failed. (check console)");
            return false;
        }
    }

    void SetInteractable(bool interactable)
    {
        if (_SignUpButton != null) _SignUpButton.interactable = interactable;
        if (_SignInButton != null) _SignInButton.interactable = interactable;

        if (_SignUpUsernameInput != null) _SignUpUsernameInput.interactable = interactable;
        if (_SignUpPasswordInput != null) _SignUpPasswordInput.interactable = interactable;

        if (_SignInUsernameInput != null) _SignInUsernameInput.interactable = interactable;
        if (_SignInPasswordInput != null) _SignInPasswordInput.interactable = interactable;
    }

    void SetStatus(string msg)
    {
        if (_StatusText != null) _StatusText.text = msg;
        Debug.Log($"[AuthUI] {msg}");
    }
}
