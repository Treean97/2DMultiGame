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
    public string Username => _PlayerIdSection != null ? _PlayerIdSection._Username : null;
    public string LoginId => _PlayerIdSection != null ? _PlayerIdSection._PlayerId : null;

    public event Action OnSignedIn;

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
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
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

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            try
            {
                await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Auth] SignIn failed: {e}");
                return false;
            }
        }
        else
        {
            Debug.Log("[Auth] SignIn skipped: already signed in.");
        }

        if (UGSCloudManager._Inst != null)
        {
            await UGSCloudManager._Inst.LoadAllAsync();
        }

        bool changed = false;

        if (_PlayerIdSection != null)
        {
            changed = _PlayerIdSection.ApplyLoginIdentity(
                loginId: username,
                defaultUsername: username
            );
        }
        else
        {
            Debug.LogWarning("[Auth] _PlayerIdSection is null. PlayerId/Username cloud record will be skipped.");
        }

        if (changed && UGSCloudManager._Inst != null)
        {
            await UGSCloudManager._Inst.SaveAllAsync();
        }

        OnSignedIn?.Invoke();
        return true;
    }
}
