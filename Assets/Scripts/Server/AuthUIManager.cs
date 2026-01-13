using System;
using System.Threading.Tasks;
using TMPro;
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
            Debug.LogError("[AuthUI] AuthManager가 씬에 없습니다.");
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

        bool ok = await AuthManager._Inst.SignUpAsync(username, password);
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
        SetInteractable(true);
        // UI 전환은 AuthManager.OnSignedIn 이벤트를 LobbyUIManager가 처리
    }

    async Task OnClickSignIn()
    {
        if (AuthManager._Inst == null)
        {
            Debug.LogError("[AuthUI] AuthManager가 씬에 없습니다.");
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
        SetInteractable(true);
        // UI 전환은 AuthManager.OnSignedIn 이벤트를 LobbyUIManager가 처리
    }

    async Task<bool> SafeSignIn(string username, string password)
    {
        try { return await AuthManager._Inst.SignInAsync(username, password); }
        catch (Exception e)
        {
            Debug.LogError($"[AuthUI] SignIn exception: {e}");
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
