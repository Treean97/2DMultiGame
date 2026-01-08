using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using UnityEngine;

public class AuthManager : MonoBehaviour
{
    public static AuthManager _Inst { get; private set; }

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
        try
        {
            await AuthenticationService.Instance
                .SignInWithUsernamePasswordAsync(username, password);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Auth] SignIn failed: {e}");
            return false;
        }
    }
}
