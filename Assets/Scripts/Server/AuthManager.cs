using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using UnityEngine;

public class AuthManager : MonoBehaviour
{
    public static AuthManager _Inst { get; private set; }

    [SerializeField] private PlayerIdSection _PlayerIdSection;

    public bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;
    public string PlayerId => AuthenticationService.Instance.PlayerId; // UGS가 발급한 고유 PlayerId

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

        // 이미 로그인 상태면 로그인 스킵 (하지만, 로컬/클라우드 기록은 규칙대로 갱신할 수 있음)
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

        // --- 여기부터는 "로그인 성공 상태" 보장 ---

        // 1) 클라우드에 기존 값이 있으면 먼저 로드해서 "최초 1회" 조건을 정확히 적용
        if (UGSCloudManager._Inst != null)
        {
            await UGSCloudManager._Inst.LoadAllAsync();
        }

        // 2) 규칙 적용
        // - 기록용 playerId = 로그인 시 입력한 id(username 입력값)
        // - username = 비어있으면 최초 1회만 playerId로 채움
        bool changed = false;

        if (_PlayerIdSection != null)
        {
            changed = _PlayerIdSection.ApplyLoginIdentity(
                loginId: username,               // 로그인 입력값 (너가 말한 "playerId로 기록할 값")
                defaultUsername: username         // username이 비어있을 때 채울 기본값(=playerId)
            );
        }
        else
        {
            Debug.LogWarning("[Auth] _PlayerIdSection is null. PlayerId/Username cloud record will be skipped.");
        }

        // 3) 변경이 있었을 때만 저장
        if (changed && UGSCloudManager._Inst != null)
        {
            await UGSCloudManager._Inst.SaveAllAsync();
        }

        return true;
    }
}
