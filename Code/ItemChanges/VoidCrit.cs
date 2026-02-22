using Mono.Cecil.Cil;
using MonoDetour;
using MonoDetour.Cil;
using MonoDetour.HookGen;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
namespace ReheatedItems.ItemChanges;


[MonoDetourTargets(typeof(HealthComponent))]
internal static class VoidCrit
{
    private const float _procChance = 1.0f;
    private const float _damageBoost = 8.0f;
    private const int _critEffectActivationCount = 3;


    [MonoDetourHookInitialize]
    private static void Setup()
    {
        if (!ConfigOptions.VoidCrit.EnableEdit.Value)
        {
            return;
        }


        ModLanguage.LangFilesToLoad.Add("VoidCrit");
        VanillaAssets.Setup();
        Hooks.Setup();
    }


    private static class VanillaAssets
    {
        private static readonly AssetReferenceT<GameObject> _voidCritEffectReference = new(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_CritGlassesVoid.CritGlassesVoidExecuteEffect_prefab);


        internal static void Setup()
        {
            AssetAsyncReferenceManager<GameObject>.LoadAsset(_voidCritEffectReference).Completed += (handle) =>
            {
                UnityEngine.Object.Destroy(handle.Result.transform.Find("FakeDamageNumbers").gameObject);
            };
        }
    }


    private static class Hooks
    {
        internal static void Setup()
        {
            Mdh.RoR2.HealthComponent.TakeDamageProcess.ILHook(RemoveVanillaFunctionality);
            GlobalEventManager.onServerDamageDealt += GlobalEventManager_onServerDamageDealt;
        }


        private static void GlobalEventManager_onServerDamageDealt(DamageReport damageReport)
        {
            if (ModUtil.IsDamageReportNull(damageReport))
            {
                return;
            }
            int voidCritCount = damageReport.attackerBody.inventory.GetItemCountEffective(DLC1Content.Items.CritGlassesVoid);
            if (voidCritCount < 1)
            {
                return;
            }
            float chanceToProc = (_procChance * voidCritCount) * damageReport.damageInfo.procCoefficient;
            if (!Util.CheckRoll(chanceToProc, damageReport.attackerMaster))
            {
                return;
            }


            EffectManager.SpawnEffect(HealthComponent.AssetReferences.critGlassesVoidExecuteEffectPrefab, new EffectData
            {
                origin = damageReport.damageInfo.position,
                scale = damageReport.victimBody.radius
            }, transmit: true);


            DamageInfo procDamageInfo = CreateVoidCritProcDamageInfo(damageReport);
            for (int i = 0; i < _critEffectActivationCount; i++)
            {
                GlobalEventManager.instance.OnCrit(damageReport.attackerBody, procDamageInfo, damageReport.attackerMaster, damageReport.damageInfo.procCoefficient, procDamageInfo.procChainMask);
            }
            damageReport.victimBody.healthComponent.TakeDamage(procDamageInfo);
        }
        private static DamageInfo CreateVoidCritProcDamageInfo(DamageReport damageReportThatProcced)
        {
            DamageInfo procDamageInfo = ModUtil.CreateNewDamageInfoFromDamageReport(damageReportThatProcced);
            procDamageInfo.damageColorIndex = DamageColorIndex.Void;
            procDamageInfo.procCoefficient = 0;


            if (procDamageInfo.damageType.IsDamageSourceSkillBased)
            {
                procDamageInfo.damageType.damageSource = DamageSource.NoneSpecified;
                procDamageInfo.damage = damageReportThatProcced.damageDealt * _damageBoost;
            }
            else
            {
                procDamageInfo.damage = damageReportThatProcced.attackerBody.damage * _damageBoost;
            }
            if (ConfigOptions.VoidCrit.UseCritMultiplier.Value)
            {
                procDamageInfo.damage *= damageReportThatProcced.attackerBody.critMultiplier;
            }


            return procDamageInfo;
        }


        private static void RemoveVanillaFunctionality(ILManipulationInfo info)
        {
            ILWeaver w = new(info);
            Instruction startOfSkip = null!;
            Instruction endOfSkip = null!;


            w.MatchRelaxed(
                x => x.MatchLdloc(0) && w.SetInstructionTo(ref startOfSkip, x),
                x => x.MatchLdfld(out _),
                x => x.MatchCallOrCallvirt<CharacterBody>("get_inventory"),
                x => x.MatchLdsfld("RoR2.DLC1Content/Items", "CritGlassesVoid"),
                x => x.MatchCallOrCallvirt<Inventory>("GetItemCountEffective")
            ).ThrowIfFailure()
            .MatchRelaxed(
                x => x.MatchLdcI4(65536),
                x => x.MatchCallOrCallvirt<DamageTypeCombo>("op_Implicit"),
                x => x.MatchCallOrCallvirt<DamageTypeCombo>("op_BitwiseOr"),
                x => x.MatchStfld<DamageInfo>("damageType") && w.SetInstructionTo(ref endOfSkip, x)
            ).ThrowIfFailure()
            .InsertBranchOver(startOfSkip, endOfSkip);
        }
    }
}