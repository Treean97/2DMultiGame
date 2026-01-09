using System;
using UnityEngine;

[Serializable]
public class PlayerIdDTO
{
    public string PlayerId;
    public string Username;
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

    // 로그인 시 호출해서 현재 플레이어 식별 정보를 갱신
    public void SetIdentity(string playerId, string username = "")
    {
        _PlayerId = playerId ?? "";
        _Username = username ?? "";
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
