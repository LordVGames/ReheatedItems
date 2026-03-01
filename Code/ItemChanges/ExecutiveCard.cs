using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoDetour;
using MonoDetour.HookGen;
using MonoDetour.Cil;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using MonoDetour.DetourTypes;
namespace ReheatedItems.ItemChanges;

[MonoDetourTargets(typeof(RoR2.Items.MultiShopCardUtils))]
[MonoDetourTargets(typeof(WolfoFixes.EquipmentFixes))]
[MonoDetourTargets(typeof(EquipmentSlot), GenerateControlFlowVariants = true)]
public static class ExecutiveCard
{
    [MonoDetourHookInitialize]
    internal static void Setup()
    {
        if (!ConfigOptions.ExecutiveCard.EnableEdit.Value)
        {
            return;
        }

        ModLanguage.LangFilesToLoad.Add("ExecutiveCard");
        CreditScoreBuff.SetupBuff();
        EquipmentDefEdits.Setup();
        Hooks.Setup();
    }


    public static class CreditScoreBuff
    {

        private static readonly AssetReferenceT<Sprite> _creditCardIconSpriteReference = new(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_MultiShopCard.texExecutiveCardIcon_png);
        public static BuffDef bdCreditScore;

        internal static void SetupBuff()
        {
            bdCreditScore = ScriptableObject.CreateInstance<BuffDef>();
            bdCreditScore.name = "bdCreditScore";
            bdCreditScore.buffColor = Color.white;
            bdCreditScore.canStack = true;
            bdCreditScore.isHidden = false;
            bdCreditScore.isDebuff = false;
            bdCreditScore.flags = BuffDef.Flags.ExcludeFromNoxiousThorns;
            bdCreditScore.ignoreGrowthNectar = true;
            // idek if this works lmao
            //bdCreditScore.startSfx = CreateNetworkSoundEventDef("Play_item_proc_moneyOnKill_loot");
            AssetAsyncReferenceManager<Sprite>.LoadAsset(_creditCardIconSpriteReference).Completed += (handle) =>
            {
                bdCreditScore.iconSprite = handle.Result;
            };
            ContentAddition.AddBuffDef(bdCreditScore);
        }

        // ty nuxlar
        private static NetworkSoundEventDef CreateNetworkSoundEventDef(string eventName)
        {
            NetworkSoundEventDef networkSoundEventDef = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            networkSoundEventDef.akId = AkSoundEngine.GetIDFromString(eventName);
            networkSoundEventDef.eventName = eventName;

            ContentAddition.AddNetworkSoundEventDef(networkSoundEventDef);

            return networkSoundEventDef;
        }
    }


    private static class EquipmentDefEdits
    {
        private static readonly AssetReferenceT<EquipmentDef> _creditCardEquipmentDefReference = new(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_MultiShopCard.MultiShopCard_asset);
        internal static void Setup()
        {
            MakeCreditCardAnActualEquipment();
        }

        private static void MakeCreditCardAnActualEquipment()
        {
            AssetAsyncReferenceManager<EquipmentDef>.LoadAsset(_creditCardEquipmentDefReference).Completed += (handle) =>
            {
                if (ConfigOptions.ExecutiveCard.AddCreditCardToBottledChaos.Value)
                {
                    handle.Result.canBeRandomlyTriggered = true;
                }
                handle.Result.enigmaCompatible = true;
                handle.Result.cooldown = 80;
            };
        }
    }


    private static class Hooks
    {
        internal static void Setup()
        {
            Mdh.RoR2.Items.MultiShopCardUtils.OnPurchase.ILHook(UseCreditScoreBuffPls);
            Mdh.RoR2.EquipmentSlot.PerformEquipmentAction.ControlFlowPrefix(EquipmentSlot_PerformEquipmentAction);
        }


        private static void UseCreditScoreBuffPls(ILManipulationInfo info)
        {
            ILWeaver w = new(info);
            ILLabel exitCode = w.DefineLabel();
            ILLabel skipFirstBadChecks = w.DefineLabel();
            ILLabel skipSecondBadChecks = w.DefineLabel();

            // going a little into a long line that starts with:
            // if (activatorMaster
            w.MatchRelaxed(
                x => x.MatchLdloc(0),
                x => x.MatchCallvirt<CharacterMaster>("get_hasBody"),
                x => x.MatchBrfalse(out exitCode) && w.SetCurrentTo(x)
            ).ThrowIfFailure()
            .InsertAfterCurrent(
                w.Create(OpCodes.Br, skipFirstBadChecks)
            );


            // going to end of same line above
            w.MatchRelaxed(
                x => x.MatchLdsfld("RoR2.DLC1Content/Equipment", "MultiShopCard"),
                x => x.MatchCallvirt<EquipmentDef>("get_equipmentIndex"),
                x => x.MatchBneUn(out _) && w.SetCurrentTo(x)
            ).ThrowIfFailure()
            .MarkLabelToCurrentNext(skipFirstBadChecks);


            // going to before:
            // if (body.equipmentSlot.stock > 0)
            w.MatchRelaxed(
                x => x.MatchLdloc(0),
                x => x.MatchCallvirt<CharacterMaster>("GetBody"),
                x => x.MatchStloc(1) && w.SetCurrentTo(x)
            ).ThrowIfFailure()
            .InsertAfterCurrent(
                w.Create(OpCodes.Ldloc_1),
                w.CreateCall(DoesBodyHaveCreditScore),
                w.Create(OpCodes.Brfalse, exitCode),
                w.Create(OpCodes.Br, skipSecondBadChecks)
            );


            // going to after the line above
            w.MatchRelaxed(
                x => x.MatchLdloc(1),
                x => x.MatchCallvirt<CharacterBody>("get_equipmentSlot"),
                x => x.MatchCallvirt<EquipmentSlot>("get_stock"),
                x => x.MatchLdcI4(0),
                x => x.MatchBle(out _) && w.SetCurrentTo(x)
            ).ThrowIfFailure()
            .MarkLabelToCurrentNext(skipSecondBadChecks)
            .CurrentToNext()
            .InsertAfterCurrent(
                w.Create(OpCodes.Ldloc_1),
                w.CreateCall(RemoveCreditScoreBuffFromBody)
            );

            //w.LogILInstructions();
        }
        private static bool DoesBodyHaveCreditScore(CharacterBody characterBody)
        {
            return characterBody.HasBuff(CreditScoreBuff.bdCreditScore);
        }
        private static void RemoveCreditScoreBuffFromBody(CharacterBody characterBody)
        {
            if (characterBody.HasBuff(CreditScoreBuff.bdCreditScore))
            {
                characterBody.RemoveBuff(CreditScoreBuff.bdCreditScore);
            }
        }




        private static ReturnFlow EquipmentSlot_PerformEquipmentAction(EquipmentSlot self, ref EquipmentDef equipmentDef, ref bool returnValue)
        {
            if (equipmentDef != DLC1Content.Equipment.MultiShopCard)
            {
                return ReturnFlow.None;
            }

            if (self.characterBody != null)
            {
                AddCreditScoreStacks(self.characterBody);
            }
            return ReturnFlow.SkipOriginal;
        }
        private static void AddCreditScoreStacks(CharacterBody characterBody)
        {
            // i really have to AddBuff on 2 separate lines................ts pmo......................................................
            characterBody.AddBuff(CreditScoreBuff.bdCreditScore);
            characterBody.AddBuff(CreditScoreBuff.bdCreditScore);
            Util.PlaySound("Play_item_proc_moneyOnKill_loot", characterBody.gameObject);
        }
    }
}