using System;
using UnityEngine;

[Serializable]
public class PlayerIdDTO
{
    public string PlayerId;   // "로그인 시 입력한 id"를 기록용으로 저장
    public string Username;   // 표시명(비어있을 때 최초 1회만 PlayerId로 초기화)
}

public class PlayerIdSection : MonoBehaviour, ICloudSection
{
    public string Key => "player_id";

    private string _PlayerId;
    private string _Username;

    void OnEnable()
    {
        if (UGSCloudManager._Inst != null)
            UGSCloudManager._Inst.Register(this);
    }

    void OnDisable()
    {
        if (UGSCloudManager._Inst != null)
            UGSCloudManager._Inst.Unregister(this);
    }

    /// <summary>
    /// 로그인 직후 호출해서 규칙을 적용합니다.
    /// - PlayerId는 항상 loginId로 갱신(기록용)
    /// - Username은 비어있는 경우에만 최초 1회 defaultUsername으로 채움
    /// </summary>
    /// <returns>값이 변경되었으면 true</returns>
    public bool ApplyLoginIdentity(string loginId, string defaultUsername)
    {
        string newPlayerId = (loginId ?? "").Trim();
        string newDefaultUsername = (defaultUsername ?? "").Trim();

        bool changed = false;

        // 1) 기록용 PlayerId는 항상 갱신
        if (_PlayerId != newPlayerId)
        {
            _PlayerId = newPlayerId;
            changed = true;
        }

        // 2) Username은 비어있을 때만 최초 1회 세팅
        if (string.IsNullOrWhiteSpace(_Username) && !string.IsNullOrWhiteSpace(newDefaultUsername))
        {
            _Username = newDefaultUsername;
            changed = true;
        }

        return changed;
    }

    public string SaveJson()
    {
        var dto = new PlayerIdDTO
        {
            PlayerId = _PlayerId ?? "",
            Username = _Username ?? "",
        };
        return JsonUtility.ToJson(dto);
    }

    public void LoadJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;

        var dto = JsonUtility.FromJson<PlayerIdDTO>(json);
        if (dto == null) return;

        _PlayerId = dto.PlayerId ?? "";
        _Username = dto.Username ?? "";

        Debug.Log($"[PlayerIdCloudData] Loaded. PlayerId={_PlayerId}, Username={_Username}");
    }
}
