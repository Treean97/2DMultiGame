using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using UnityEngine;

public class AuthManager : MonoBehaviour
{
    public static AuthManager _Inst { get; private set; }

    [SerializeField] private PlayerIdSection _PlayerIdSection;

    public bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;
    public string PlayerId => AuthenticationService.Instance.PlayerId;

    void Awake()
    {
        if (_Inst != null) { Destroy(gameObject); return; }
        _Inst = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task<bool> SignUpAsync(string username, string password)
    {
        await UgsBootstrap.EnsureInitAsync();
        try
        {
            await AuthenticationService.Instance
                .SignUpWithUsernamePasswordAsync(username, password);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Auth] SignUp failed: {e}");
            return false;
        }
    }

    public async Task<bool> SignInAsync(string username, string password)
    {
        await UgsBootstrap.EnsureInitAsync();

        // 이미 로그인 상태면 로그인 스킵
        if (AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("[Auth] SignIn skipped: already signed in.");

            if (_PlayerIdSection != null)
                _PlayerIdSection.SetIdentity(AuthenticationService.Instance.PlayerId, username);

            if (UGSCloudManager._Inst != null)
                await UGSCloudManager._Inst.SaveAllAsync();

            return true;
        }

        try
        {
            await AuthenticationService.Instance
                .SignInWithUsernamePasswordAsync(username, password);

            if (_PlayerIdSection != null)
                _PlayerIdSection.SetIdentity(AuthenticationService.Instance.PlayerId, username);

            if (UGSCloudManager._Inst != null)
                await UGSCloudManager._Inst.SaveAllAsync();

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Auth] SignIn failed: {e}");
            return false;
        }
    }


}
