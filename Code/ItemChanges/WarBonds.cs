using Mdh.RoR2.BarrageOnBossBehaviour;
using Mono.Cecil.Cil;
using MonoDetour;
using MonoDetour.Cil;
using MonoDetour.DetourTypes;
using MonoDetour.HookGen;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Projectile;
using RoR2BepInExPack.Utilities;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
namespace ReheatedItems.ItemChanges;


[MonoDetourTargets(typeof(BarrageOnBossBehaviour), GenerateControlFlowVariants = true)]
[MonoDetourTargets(typeof(MoneyPickup))]
[MonoDetourTargets(typeof(ProjectileKnockOutGold))]
[MonoDetourTargets(typeof(CharacterBody), Members = ["OnInventoryChanged"])]
public static class WarBonds
{
    public const int WarBondsGoldChunkDropChance = 20;
    public const int WarBondsGoldChunksNeededCount = 5;
    public const int WarBondsMaximumStoredMissiles = 5;
    public const float WarBondsNewDelayBeforeMissiles = 2;
    public const float WarBondsNewDelayBetweenMissiles = 0.2f;
    // missile hits twice on ground enemies for 2x coefficient
    public const float WarBondsMissileDamageCoefficientInitial = 8;
    public const float WarBondsMissileDamageCoefficientStacked = 6;


    private static BarrageOnBossBehaviour barrageOnBossBehaviourInstance;
    private static readonly AssetReferenceT<BuffDef> _warbondsBuffDefAssetReference = new(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2.bdExtraBossMissile_asset);


    private static readonly FixedConditionalWeakTable<CharacterBody, RiWarBondsAttackerInfo> _riWarBondsAttackerInfoTable = [];
    private class RiWarBondsAttackerInfo
    {
        internal float DamagePerMissile;
    }


    [MonoDetourHookInitialize]
    private static void Setup()
    {
        ModLanguage.LangFilesToLoad.Add("WarBonds");
        WarBondsBuildUpBuff.SetupBuff();

        Mdh.RoR2.BarrageOnBossBehaviour.Start.ControlFlowPrefix(RemoveVanillaBehavior);
        Mdh.RoR2.BarrageOnBossBehaviour.UpdateExtraMissileMoneyCount.ControlFlowPrefix(RemoveVanillaBehaviorAgain);
        Mdh.RoR2.MoneyPickup.OnTriggerStay.ILHook(MoneyPickup_OnTriggerStay);
        Mdh.RoR2.CharacterBody.OnInventoryChanged.Postfix(OnInventoryChanged);
        Mdh.RoR2.BarrageOnBossBehaviour.Start.Postfix(BarrageOnBossBehaviour_Start);
        Mdh.RoR2.BarrageOnBossBehaviour.CalculateMissileDamage.ControlFlowPrefix(BarrageOnBossBehaviour_CalculateMissileDamage);
        Mdh.RoR2.BarrageOnBossBehaviour.UpdateBarrage.Postfix(BarrageOnBossBehaviour_UpdateBarrage);
    }


    private static ReturnFlow RemoveVanillaBehavior(BarrageOnBossBehaviour self)
    {
        return ReturnFlow.SkipOriginal;
    }


    private static ReturnFlow RemoveVanillaBehaviorAgain(BarrageOnBossBehaviour self, ref float amount)
    {
        return ReturnFlow.SkipOriginal;
    }


    private static void MoneyPickup_OnTriggerStay(ILManipulationInfo info)
    {
        ILWeaver w = new(info);

        w.MatchRelaxed(
            x => x.MatchLdloc(2),
            x => x.MatchLdcI4(1),
            x => x.MatchCallOrCallvirt<CharacterBody>("OnPickup") && w.SetCurrentTo(x)
        ).ThrowIfFailure()
        .InsertAfterCurrent(
            w.Create(OpCodes.Ldloc_2),
            w.CreateDelegateCall(OnGoldChunkPickedUp)
        );
    }
    private static void OnGoldChunkPickedUp(CharacterBody characterBody)
    {
        if (characterBody == null || characterBody.inventory == null)
        {
            return;
        }
        int warbondsItemCount = characterBody.inventory.GetItemCountEffective(DLC2Content.Items.BarrageOnBoss);
        if (warbondsItemCount < 1)
        {
            return;
        }
        int warbondsBuffCount = characterBody.GetBuffCount(DLC2Content.Buffs.ExtraBossMissile);
        if (warbondsBuffCount >= WarBondsMaximumStoredMissiles)
        {
            return;
        }
        int warbondsBuildUpBuffCount = characterBody.GetBuffCount(WarBondsBuildUpBuff.bdWarBondsBuildUp);


        if (warbondsBuildUpBuffCount >= WarBondsGoldChunksNeededCount - 1)
        {
            characterBody.AddBuff(DLC2Content.Buffs.ExtraBossMissile);
            for (int i = 0; i < warbondsBuildUpBuffCount; i++)
            {
                characterBody.RemoveBuff(WarBondsBuildUpBuff.bdWarBondsBuildUp);
            }
        }
        else
        {
            characterBody.AddBuff(WarBondsBuildUpBuff.bdWarBondsBuildUp);
        }
    }


    private static void OnInventoryChanged(CharacterBody self)
    {
        if (NetworkServer.active && self != null && self.inventory != null)
        {
            self.AddItemBehavior<ChanceDropGoldOnHitBehavior>(self.inventory.GetItemCountEffective(DLC2Content.Items.BarrageOnBoss));
        }
    }


    private static void BarrageOnBossBehaviour_Start(BarrageOnBossBehaviour self)
    {
        self.initialMissileDelay = WarBondsNewDelayBeforeMissiles;
        self.fireDelay = WarBondsNewDelayBetweenMissiles;
        //self.missileBarrageCount = 1;
        barrageOnBossBehaviourInstance = self;
    }


    private static ReturnFlow BarrageOnBossBehaviour_CalculateMissileDamage(BarrageOnBossBehaviour self, ref CharacterBody targetBody, ref float returnValue)
    {
        if (!_riWarBondsAttackerInfoTable.TryGetValue(self.body, out var riWarBondsAttackerInfo))
        {
            return ReturnFlow.SkipOriginal;
        }

        returnValue = riWarBondsAttackerInfo.DamagePerMissile;
        return ReturnFlow.SkipOriginal;
    }


    private static void BarrageOnBossBehaviour_UpdateBarrage(BarrageOnBossBehaviour self)
    {
        if (self.barrageQuantity < 1 && _riWarBondsAttackerInfoTable.TryGetValue(self.body, out _))
        {
            _riWarBondsAttackerInfoTable.Remove(self.body);
            barrageOnBossBehaviourInstance.currentEnemy = null;
        }
    }




    public static class WarBondsBuildUpBuff
    {
        public static BuffDef bdWarBondsBuildUp;

        internal static void SetupBuff()
        {
            bdWarBondsBuildUp = ScriptableObject.CreateInstance<BuffDef>();
            bdWarBondsBuildUp.name = "bdWarBondsBuildUp";
            bdWarBondsBuildUp.isHidden = false;
            bdWarBondsBuildUp.canStack = true;
            bdWarBondsBuildUp.isDebuff = false;
            bdWarBondsBuildUp.flags = BuffDef.Flags.ExcludeFromNoxiousThorns;
            bdWarBondsBuildUp.ignoreGrowthNectar = true;
            bdWarBondsBuildUp.buffColor = Color.grey;
            AssetAsyncReferenceManager<BuffDef>.LoadAsset(_warbondsBuffDefAssetReference).Completed += (handle) =>
            {
                bdWarBondsBuildUp.iconSprite = handle.Result.iconSprite;
            };
            ContentAddition.AddBuffDef(bdWarBondsBuildUp);
        }
    }


    // extending the gilded aspect so i can use it's temp gold chunk dropping method (because it's not static!!!!!!!)
    private class ChanceDropGoldOnHitBehavior : AffixAurelioniteBehavior
    {
        private new void Start()
        {
            EnsureReferences();
        }


        private new void OnEnable()
        {
            GlobalEventManager.onServerDamageDealt += WarBondsOnServerDamageDealt;
        }


        private new void OnDisable()
        {
            GlobalEventManager.onServerDamageDealt -= WarBondsOnServerDamageDealt;
        }


        private void WarBondsOnServerDamageDealt(DamageReport damageReport)
        {
            if (ModUtil.IsDamageReportNull(damageReport))
            {
                return;
            }
            int warbondsItemCount = damageReport.attackerBody.inventory.GetItemCountEffective(DLC2Content.Items.BarrageOnBoss);
            if (warbondsItemCount < 1)
            {
                return;
            }




            float goldChunkDropChance = WarBondsGoldChunkDropChance * damageReport.damageInfo.procCoefficient;
            if (Util.CheckRoll(goldChunkDropChance, damageReport.attackerMaster))
            {
                // re-using the gilded aspect on hit method to drop a temporary gold chunk
                OnServerDamageDealt(damageReport);
            }


            int warBondsMissileCount = damageReport.attackerBody.GetBuffCount(DLC2Content.Buffs.ExtraBossMissile);
            if (warBondsMissileCount < 1)
            {
                return;
            }


            bool canSkillAttackProcBands = damageReport.damageInfo.damage / damageReport.attackerBody.damage >= 4f && damageReport.damageInfo.damageType.IsDamageSourceSkillBased;
            if (barrageOnBossBehaviourInstance.currentEnemy == null && canSkillAttackProcBands)
            {
                if (!_riWarBondsAttackerInfoTable.TryGetValue(damageReport.attackerBody, out _))
                {
                    float missileDamage = Util.OnHitProcDamage(damageReport.damageInfo.damage, damageReport.attackerBody.damage, GetTotalWarBondsDamageCoeff(damageReport.attackerBody));
                    _riWarBondsAttackerInfoTable.Add(damageReport.attackerBody, new RiWarBondsAttackerInfo { DamagePerMissile = missileDamage });
                }
                barrageOnBossBehaviourInstance.currentEnemy = damageReport.victimBody;
                barrageOnBossBehaviourInstance.StartMissileCountdown(true);
            }
        }
    }


    private static float GetTotalWarBondsDamageCoeff(CharacterBody attackerBody)
    {
        if (attackerBody == null || attackerBody.inventory == null)
        {
            return 0;
        }
        int warBondsItemCount = attackerBody.inventory.GetItemCountEffective(DLC2Content.Items.BarrageOnBoss);
        if (warBondsItemCount < 1)
        {
            return 0;
        }


        return WarBondsMissileDamageCoefficientInitial + (WarBondsMissileDamageCoefficientStacked * (warBondsItemCount - 1));
    }
}