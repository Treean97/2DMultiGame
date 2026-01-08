using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class AuthUIManager : MonoBehaviour
{
    [Header("Auth UI")]
    [SerializeField] private TMP_InputField _UsernameInput;
    [SerializeField] private TMP_InputField _PasswordInput;
    [SerializeField] private Button _SignUpButton;
    [SerializeField] private Button _SignInButton;
    [SerializeField] private TMP_Text _StatusText;

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

        string username = _UsernameInput != null ? _UsernameInput.text : "";
        string password = _PasswordInput != null ? _PasswordInput.text : "";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            SetStatus("SignUp: username/password is empty.");
            return;
        }

        SetInteractable(false);
        SetStatus("Signing up...");

        bool ok;
        try
        {
            ok = await AuthManager._Inst.SignUpAsync(username, password);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AuthUI] SignUp exception: {e}");
            ok = false;
        }

        if (!ok)
        {
            SetStatus("SignUp failed. (check console)");
            SetInteractable(true);
            return;
        }

        if (!AuthManager._Inst.IsSignedIn)
        {
            SetStatus("SignUp ok. Signing in...");
            bool signedIn = await AuthManager._Inst.SignInAsync(username, password);
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

        string username = _UsernameInput != null ? _UsernameInput.text : "";
        string password = _PasswordInput != null ? _PasswordInput.text : "";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            SetStatus("SignIn: username/password is empty.");
            return;
        }

        SetInteractable(false);
        SetStatus("Signing in...");

        bool ok;
        try
        {
            ok = await AuthManager._Inst.SignInAsync(username, password);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AuthUI] SignIn exception: {e}");
            ok = false;
        }

        if (!ok)
        {
            SetStatus("SignIn failed. (check console)");
            SetInteractable(true);
            return;
        }

        SetStatus($"Auth ok. PlayerId={AuthManager._Inst.PlayerId}");
        await LogLobbyConnectionAsync();

        SetInteractable(true);
    }

    async Task LogLobbyConnectionAsync()
    {
        try
        {
            await UgsBootstrap.EnsureInitAsync();

            var options = new QueryLobbiesOptions { Count = 1 };
            var res = await LobbyService.Instance.QueryLobbiesAsync(options);

            Debug.Log($"[AuthUI] 로비 서비스 접속 성공 (Query OK). results={res?.Results?.Count ?? 0}");
            SetStatus("Lobby connected. (Query OK)");
        }
        catch (Exception e)
        {
            Debug.LogError($"[AuthUI] 로비 서비스 접속 실패: {e}");
            SetStatus("Lobby connect failed. (check console)");
        }
    }

    void SetInteractable(bool interactable)
    {
        if (_SignUpButton != null) _SignUpButton.interactable = interactable;
        if (_SignInButton != null) _SignInButton.interactable = interactable;
        if (_UsernameInput != null) _UsernameInput.interactable = interactable;
        if (_PasswordInput != null) _PasswordInput.interactable = interactable;
    }

    void SetStatus(string msg)
    {
        if (_StatusText != null) _StatusText.text = msg;
        Debug.Log($"[AuthUI] {msg}");
    }
}
