using BepInEx;
using MiscFixes.Modules;
using MonoDetour;
using R2API;
using System;
[assembly: HG.Reflection.SearchableAttribute.OptIn]
namespace ReheatedItems;


[BepInDependency(RocketSurvivor.RocketSurvivorPlugin.MODUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(ModSupport.RiskyTweaksMod.RiskyTweaksMod.GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(ModSupport.WolfFixes.WolfFixesMod.ModGUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(ItemAPI.PluginGUID)]
[BepInDependency(LanguageAPI.PluginGUID)]
[BepInDependency(ProcTypeAPI.PluginGUID)]
[BepInDependency(RecalculateStatsAPI.PluginGUID)]
[BepInDependency(PrefabAPI.PluginGUID)]
[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    public static PluginInfo PluginInfo { get; private set; }
    public void Awake()
    {
        PluginInfo = Info;
        Log.Init(Logger);
        ConfigOptions.BindAllConfigOptions(Config);
        MonoDetourManager.InvokeHookInitializers(typeof(Plugin).Assembly, reportUnloadableTypes: false);
        ModLanguage.AddNewLangTokens();
    }
}