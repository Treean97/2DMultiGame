using System.Threading.Tasks;
using Unity.Services.Core;

public static class UgsBootstrap
{
    static bool _Initialized;

    public static async Task EnsureInitAsync()
    {
        if (_Initialized) return;
        await UnityServices.InitializeAsync();
        _Initialized = true;
    }
}
