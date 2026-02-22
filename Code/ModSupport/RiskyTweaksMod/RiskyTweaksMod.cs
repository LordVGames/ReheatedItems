using System;
using System.Collections.Generic;
using System.Text;
namespace ReheatedItems.ModSupport.RiskyTweaksMod;


internal static class RiskyTweaksMod
{
    internal const string GUID = "com.Moffein.RiskyTweaks";
    private static bool? _enabled;

    internal static bool ModIsRunning
    {
        get
        {
            _enabled ??= BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GUID);
            return (bool)_enabled;
        }
    }
}