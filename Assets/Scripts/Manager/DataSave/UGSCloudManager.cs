using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using UnityEngine;

public class UGSCloudManager : MonoBehaviour
{
    public static UGSCloudManager _Inst { get; private set; }

    // 등록된 섹션 (key 중복이면 최신 등록으로 교체)
    readonly Dictionary<string, ICloudSection> _Sections = new();

    // JSON 캐시
    readonly Dictionary<string, string> _LoadedJsonCache = new();

    void Awake()
    {
        if (_Inst != null) { Destroy(gameObject); return; }
        _Inst = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Register(ICloudSection section)
    {
        if (section == null) return;
        if (string.IsNullOrWhiteSpace(section.Key))
        {
            Debug.LogError("[UGSCloudData] Register failed: section.Key is null/empty.");
            return;
        }

        _Sections[section.Key] = section;

        // 이미 로드된 값이 있으면, 늦게 등록된 섹션에도 즉시 반영
        if (_LoadedJsonCache.TryGetValue(section.Key, out string json))
        {
            try { section.LoadJson(json); }
            catch (Exception e)
            {
                Debug.LogError($"[UGSCloudData] Apply cached LoadJson failed. key={section.Key}, err={e}");
            }
        }
    }

    public void Unregister(ICloudSection section)
    {
        if (section == null) return;
        if (string.IsNullOrWhiteSpace(section.Key)) return;

        if (_Sections.TryGetValue(section.Key, out var current) && ReferenceEquals(current, section))
            _Sections.Remove(section.Key);
    }

    public async Task<bool> SaveAllAsync()
    {
        if (!await EnsureReadyAsync()) return false;

        try
        {
            var payload = new Dictionary<string, object>(_Sections.Count);

            foreach (var kv in _Sections)
            {
                string key = kv.Key;
                ICloudSection section = kv.Value;

                string json;
                try
                {
                    json = section.SaveJson();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UGSCloudData] SaveJson failed. key={key}, err={e}");
                    continue;
                }

                // value는 JSON 문자열로 통일
                payload[key] = json ?? "";
            }

            if (payload.Count == 0)
            {
                Debug.Log("[UGSCloudData] SaveAll skipped: no payload.");
                return true;
            }

            await CloudSaveService.Instance.Data.Player.SaveAsync(payload);
            Debug.Log($"[UGSCloudData] SaveAll OK. keys={payload.Count}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[UGSCloudData] SaveAll failed: {e}");
            return false;
        }
    }

    public async Task<bool> LoadAllAsync()
    {
        if (!await EnsureReadyAsync()) return false;

        try
        {
            if (_Sections.Count == 0)
            {
                Debug.Log("[UGSCloudData] LoadAll skipped: no sections registered.");
                return true;
            }

            var keys = new HashSet<string>(_Sections.Keys);
            var res = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            foreach (var key in keys)
            {
                if (!res.TryGetValue(key, out var item))
                    continue;

                string json;
                try
                {
                    json = item.Value.GetAs<string>();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UGSCloudData] GetAs<string>() failed. key={key}, err={e}");
                    continue;
                }

                _LoadedJsonCache[key] = json;

                if (_Sections.TryGetValue(key, out var section))
                {
                    try { section.LoadJson(json); }
                    catch (Exception e)
                    {
                        Debug.LogError($"[UGSCloudData] LoadJson failed. key={key}, err={e}");
                    }
                }
            }

            Debug.Log($"[UGSCloudData] LoadAll OK. received={res?.Count ?? 0}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[UGSCloudData] LoadAll failed: {e}");
            return false;
        }
    }

    async Task<bool> EnsureReadyAsync()
    {
        try
        {
            await UgsBootstrap.EnsureInitAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.LogError("[UGSCloudData] Not signed in. Cloud Save requires authentication.");
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[UGSCloudData] EnsureReady failed: {e}");
            return false;
        }
    }
}
