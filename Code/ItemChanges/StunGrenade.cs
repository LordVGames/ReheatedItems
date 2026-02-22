using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoDetour;
using MonoDetour.Cil;
using MonoDetour.HookGen;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
namespace ReheatedItems.ItemChanges;


[MonoDetourTargets]
public static class StunGrenade
{
    private const float _procChance = 5f;
    private const float _damageCoefficient = 2.0f;
    private const float _attackSpeedReduction = 0.3f;
    private const float _moveSpeedReduction = 0.2f;
    private const float _damageReduction = 0.15f;


    [MonoDetourHookInitialize]
    private static void Setup()
    {
        if (!ConfigOptions.StunGrenade.EnableEdit.Value)
        {
            return;
        }


        
        ModLanguage.LangFilesToLoad.Add("StunGrenade");
        VanillaAssets.Setup();
        ModAssets.Setup();
        DazeBuff.SetupBuff();
    }


    private static class VanillaAssets
    {
        private static readonly AssetReferenceT<ItemDef> _stunGrenadeItemDefReference = new(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_StunChanceOnHit.StunChanceOnHit_asset);
        private static readonly AssetReferenceT<ItemTierDef> _tier2ItemTierDefReference = new(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common.Tier2Def_asset);
        internal static ItemTierDef _greenItemTier;
        private static readonly AssetReferenceT<GameObject> _stunGrenadeEffectReference = new(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_StunChanceOnHit.ImpactStunGrenade_prefab);
        internal static GameObject StunGrenadeEffect;


        internal static void Setup()
        {
            AssetAsyncReferenceManager<ItemTierDef>.LoadAsset(_tier2ItemTierDefReference).Completed += (handle) =>
            {
                _greenItemTier = handle.Result;
            };
            AssetAsyncReferenceManager<ItemDef>.LoadAsset(_stunGrenadeItemDefReference).Completed += (handle) =>
            {
                handle.Result.pickupIconSprite = ModAssets.StunGrenadeIconGreen;
                handle.Result.tier = ItemTier.Tier2;
                handle.Result._itemTierDef = _greenItemTier;
#pragma warning disable CS0618 // Type or member is obsolete
                handle.Result.deprecatedTier = ItemTier.Tier2;
#pragma warning restore CS0618 // Type or member is obsolete
            };
            AssetAsyncReferenceManager<GameObject>.LoadAsset(_stunGrenadeEffectReference).Completed += (handle) =>
            {
                StunGrenadeEffect = handle.Result;
            };
        }
    }


    private static class ModAssets
    {
        internal static AssetBundle StunGrenadeAssetBundle;
        internal const string StunGrenadeAssetBundleName = "stungrenade";
        internal static string StunGrenadeAssetBundlePath
        {
            get
            {
                return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Plugin.PluginInfo.Location), StunGrenadeAssetBundleName);
            }
        }
        internal static void Setup()
        {
            StunGrenadeAssetBundle = AssetBundle.LoadFromFile(StunGrenadeAssetBundlePath);
            LoadAssets();
        }




        internal static Sprite StunGrenadeIconGreen;


        internal static void LoadAssets()
        {
            StunGrenadeIconGreen = StunGrenadeAssetBundle.LoadAsset<Sprite>("texStunGrenadeGreenIcon");
        }
    }


    public static class DazeBuff
    {
        public static BuffDef bdStunGrenadeDaze;

        internal static void SetupBuff()
        {
            bdStunGrenadeDaze = ScriptableObject.CreateInstance<BuffDef>();
            bdStunGrenadeDaze.name = "bdStunGrenadeDaze";
            bdStunGrenadeDaze.isHidden = false;
            bdStunGrenadeDaze.canStack = false;
            bdStunGrenadeDaze.isDebuff = true;
            bdStunGrenadeDaze.flags = BuffDef.Flags.IncludeInRandomBuff;
            bdStunGrenadeDaze.ignoreGrowthNectar = true;
            bdStunGrenadeDaze.buffColor = Color.white;
            bdStunGrenadeDaze.iconSprite = ModAssets.StunGrenadeIconGreen;
            ContentAddition.AddBuffDef(bdStunGrenadeDaze);
        }
    }


    public static class ProcType
    {
        public static ModdedProcType StunGrenadeProcced = ProcTypeAPI.ReserveProcType();
    }


    [MonoDetourTargets(typeof(SetStateOnHurt))]
    private static class Hooks
    {
        [MonoDetourHookInitialize]
        private static void Setup()
        {
            Mdh.RoR2.SetStateOnHurt.OnTakeDamageServer.ILHook(OnTakeDamageServer);
            // normal stun grenade can only proc if the state can be changed on hurt, so it wouldn't work on bosses unless we make our own onServerDamageDealt
            GlobalEventManager.onServerDamageDealt += GlobalEventManager_onServerDamageDealt;
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private static void GlobalEventManager_onServerDamageDealt(DamageReport damageReport)
        {
            if (ModUtil.IsDamageReportNull(damageReport))
            {
                return;
            }
            if (damageReport.damageInfo.procChainMask.HasModdedProc(ProcType.StunGrenadeProcced))
            {
                return;
            }
            int stunGrenadeCount = damageReport.attackerBody.inventory.GetItemCountEffective(RoR2Content.Items.StunChanceOnHit);
            if (stunGrenadeCount < 1)
            {
                return;
            }
            if (!Util.CheckRoll(((_procChance * stunGrenadeCount) * damageReport.damageInfo.procCoefficient), damageReport.attackerMaster))
            {
                return;
            }


            EffectManager.SimpleImpactEffect(VanillaAssets.StunGrenadeEffect, damageReport.damageInfo.position, -damageReport.damageInfo.force, transmit: true);
            DamageInfo procDamageInfo = ModUtil.CreateNewDamageInfoFromDamageReport(damageReport);
            procDamageInfo.damage *= _damageCoefficient;
            procDamageInfo.procChainMask.AddModdedProc(ProcType.StunGrenadeProcced);


            damageReport.victimBody.healthComponent.TakeDamage(procDamageInfo);
            damageReport.victimBody.AddTimedBuff(DazeBuff.bdStunGrenadeDaze, 8);
        }


        private static void OnTakeDamageServer(ILManipulationInfo info)
        {
            ILWeaver w = new(info);
            Instruction startOfSkip = null!;
            Instruction endOfSkip = null!;


            w.MatchRelaxed(
                x => x.MatchLdloc(2) && w.SetInstructionTo(ref startOfSkip, x),
                x => x.MatchCallOrCallvirt(out _),
                x => x.MatchBrtrue(out _),
                x => x.MatchLdcI4(0),
                x => x.MatchBr(out _),
                x => x.MatchLdloc(2),
                x => x.MatchCallOrCallvirt<CharacterMaster>("get_inventory"),
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "StunChanceOnHit")
            ).ThrowIfFailure()
            .MatchRelaxed(
                x => x.MatchLdcI4(1),
                x => x.MatchCallOrCallvirt("RoR2.EffectManager", "SimpleImpactEffect"),
                x => x.MatchLdarg(0),
                x => x.MatchLdcR4(2),
                x => x.MatchCallOrCallvirt<SetStateOnHurt>("SetStun") && w.SetInstructionTo(ref endOfSkip, x) && w.SetCurrentTo(x)
            ).ThrowIfFailure()
            .InsertBranchOver(startOfSkip, endOfSkip);
        }


        private static void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender == null || !sender.HasBuff(DazeBuff.bdStunGrenadeDaze))
            {
                return;
            }


            args.attackSpeedReductionMultAdd += _attackSpeedReduction;
            args.moveSpeedReductionMultAdd += _moveSpeedReduction;
            // is this the best way to do this?
            args.damageMultAdd -= _damageReduction;
        }
    }
}