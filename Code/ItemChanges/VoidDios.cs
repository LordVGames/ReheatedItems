using MiscFixes.Modules;
using Mono.Cecil.Cil;
using MonoDetour;
using MonoDetour.Cil;
using MonoDetour.DetourTypes;
using MonoDetour.HookGen;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.CharacterAI;
using RoR2.ContentManagement;
using RoR2.Items;
using RoR2BepInExPack.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using static UnityEngine.Object;
namespace ReheatedItems.ItemChanges;


[MonoDetourTargets(typeof(ExtraLifeVoidManager), GenerateControlFlowVariants = true)]
[MonoDetourTargets(typeof(CharacterMaster))]
[MonoDetourTargets(typeof(CharacterBody), GenerateControlFlowVariants = true)]
internal static class VoidDios
{
    #region Needed Assets
    private static readonly AssetReferenceT<ItemDef> _voidDiosItemAssetReference = new(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_ExtraLifeVoid.ExtraLifeVoid_asset);
    private static readonly AssetReferenceT<ItemDef> _cutHpItemDefAssetReference = new(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_CutHp.CutHp_asset);
    private static ItemDef _cutHpItemDef;

    private static readonly AssetReferenceT<GameObject> _reaverAllyBodyPrefabReference = new(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Nullifier.NullifierAllyBody_prefab); 

    private static readonly AssetReferenceT<GameObject> _jailerAllyBodyPrefabReference = new(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidJailer.VoidJailerAllyBody_prefab);

    private static readonly AssetReferenceT<GameObject> _devastatorAllyBodyPrefabReference = new(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidMegaCrab.VoidMegaCrabAllyBody_prefab);
    #endregion


    private static readonly FixedConditionalWeakTable<CharacterMaster, RiVoidDiosInfo> _riVoidDiosInfoTable = [];
    private class RiVoidDiosInfo
    {
        internal GameObject OriginalBodyPrefab;
        internal string NameOfSceneFirstRevivedIn;
    }


    private const float _respawnImmunityTime = 5f;


    [MonoDetourHookInitialize]
    internal static void Setup()
    {
        if (!ConfigOptions.VoidDios.EnableEdit.Value)
        {
            return;
        }

        EditVoidBodyPrefabs();
        EditItemPrefabs();
        // the method to pick a void guy to respawn as is still there, just unused
        Mdh.RoR2.Items.ExtraLifeVoidManager.GetNextBodyPrefab.ControlFlowPrefix(ExtraLifeVoidManager_GetNextBodyPrefab);
        Mdh.RoR2.CharacterMaster.RespawnExtraLifeVoid.ILHook(CharacterMaster_RespawnExtraLifeVoid);
        Mdh.RoR2.CharacterBody.Start.ControlFlowPrefix(CharacterBody_Start);
        ModLanguage.LangFilesToLoad.Add("VoidDios");
    }


    private static void EditVoidBodyPrefabs()
    {
        // void allies are playable now so i need to increase all of their interaction ranges since they can't reach anything normally
        AssetAsyncReferenceManager<GameObject>.LoadAsset(_reaverAllyBodyPrefabReference).Completed += (handle) =>
        {
            Interactor reaverAllyInteractor = handle.Result.GetComponent<Interactor>();
            reaverAllyInteractor.maxInteractionDistance = 9;
        };

        AssetAsyncReferenceManager<GameObject>.LoadAsset(_jailerAllyBodyPrefabReference).Completed += (handle) =>
        {
            Interactor jailerAllyInteractor = handle.Result.GetComponent<Interactor>();
            jailerAllyInteractor.maxInteractionDistance = 12;
        };

        AssetAsyncReferenceManager<GameObject>.LoadAsset(_devastatorAllyBodyPrefabReference).Completed += (handle) =>
        {
            Interactor devastatorAllyInteractor = handle.Result.GetComponent<Interactor>();
            devastatorAllyInteractor.maxInteractionDistance = 15;
        };
    }


    private static void EditItemPrefabs()
    {
        // no way i'm letting engi turrets get this new void dios lmao
        AssetAsyncReferenceManager<ItemDef>.LoadAsset(_voidDiosItemAssetReference).Completed += (handle) =>
        {
            ItemDef voidDiosItemDef = handle.Result;
#if !DEBUG
            voidDiosItemDef.tags = [.. voidDiosItemDef.tags, ItemTag.CannotCopy];
#endif
        };

        // why tf is this not already set as hidden
        AssetAsyncReferenceManager<ItemDef>.LoadAsset(_cutHpItemDefAssetReference).Completed += (handle) =>
        {
            _cutHpItemDef = handle.Result;
            _cutHpItemDef.hidden = true;
        };
    }




    private static ReturnFlow ExtraLifeVoidManager_GetNextBodyPrefab(ref GameObject returnValue)
    {
        string[] bodyList = ConfigOptions.VoidDios.RespawnableBodiesList.Value.Split(',');
        Log.Info($"Void dios body list length is {bodyList.Length}");
        if (bodyList.Length < 1)
        {
            Log.Error("No bodies specified to respawn as for void dios!");
            returnValue = null;
            return ReturnFlow.SkipOriginal;
        }


        int bodyListRandomIndex = ExtraLifeVoidManager.rng.RangeInt(0, bodyList.Length);
        Log.Info($"Chosen position in body list is {bodyListRandomIndex}");
        GameObject chosenBody = BodyCatalog.FindBodyPrefab(bodyList[bodyListRandomIndex]);
        if (chosenBody == null)
        {
            Log.Warning($"Body named {bodyList[bodyListRandomIndex]} at position {bodyListRandomIndex} is not a valid body! Picking from the first valid result instead...");
            foreach (string bodyName in bodyList)
            {
                GameObject newChosenBody = BodyCatalog.FindBodyPrefab(bodyName);
                if (newChosenBody != null)
                {
                    chosenBody = newChosenBody;
                    break;
                }
            }
            if (chosenBody == null)
            {
                Log.Error("A valid body could not be found from the list provided for void dios!");
                returnValue = null;
                return ReturnFlow.SkipOriginal;
            }
        }


        returnValue = chosenBody;
        return ReturnFlow.SkipOriginal;
    }


    private static void CharacterMaster_RespawnExtraLifeVoid(ILManipulationInfo info)
    {
        ILWeaver w = new(info);


        // going into start of line:
        // Respawn(vector, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), wasRevivedMidStage: true);
        w.MatchRelaxed(
            x => x.MatchLdarg(0) && w.SetCurrentTo(x),
            x => x.MatchLdloc(0),
            x => x.MatchLdcR4(0),
            x => x.MatchLdcR4(0)
        ).ThrowIfFailure()
        .InsertBeforeCurrentStealLabels(
            w.Create(OpCodes.Ldarg_0),
            w.CreateDelegateCall((CharacterMaster characterMaster) =>
            {
                if (characterMaster == null || characterMaster.GetBody() == null)
                {
                    return;
                }
                Log.Info($"characterMaster is {characterMaster}");
                if (!_riVoidDiosInfoTable.TryGetValue(characterMaster, out _))
                {
                    _riVoidDiosInfoTable.Add(characterMaster, new RiVoidDiosInfo { OriginalBodyPrefab = characterMaster.bodyPrefab, NameOfSceneFirstRevivedIn = SceneManager.GetActiveScene().name });
                    Log.Info($"Giving {characterMaster} (a {characterMaster.GetBody().name}) a CutHp due to them using up a void dios for the first time this stage");
                    characterMaster.inventory?.GiveItemPermanent(_cutHpItemDef.itemIndex);
                }


                GameObject newBodyPrefab = ExtraLifeVoidManager.GetNextBodyPrefab();
                if (newBodyPrefab == null)
                {
                    return;
                }
                characterMaster.bodyPrefab = newBodyPrefab;
                BaseAI originalBaseAI = characterMaster.GetComponent<BaseAI>();
                if (originalBaseAI != null)
                {
                    BodyIndex bodyIndexFromPrefab = BodyCatalog.FindBodyIndex(newBodyPrefab);
                    Log.Debug(bodyIndexFromPrefab);
                    MasterCatalog.MasterIndex masterIndexFromBodyIndex = MasterCatalog.FindAiMasterIndexForBody(bodyIndexFromPrefab);
                    Log.Debug(masterIndexFromBodyIndex);
                    GameObject voidAllyMasterPrefab = MasterCatalog.GetMasterPrefab(masterIndexFromBodyIndex);
                    Log.Debug(voidAllyMasterPrefab);
                    ReplaceAISkillDrivers(characterMaster, originalBaseAI, voidAllyMasterPrefab); // fixes the ai being lobotomized since the ai doesn't change with their body
                }
            })
        );


        // going in middle of line:
        // GetBody().AddTimedBuff(RoR2Content.Buffs.Immune, 3f);
        w.MatchRelaxed(
            x => x.MatchLdsfld("RoR2.RoR2Content/Buffs", "Immune"),
            x => x.MatchLdcR4(3) && w.SetCurrentTo(x)
        ).ThrowIfFailure()
        .InsertAfterCurrent(
            w.CreateDelegateCall((float oldInvulnerabilityTime) =>
            {
                return _respawnImmunityTime;
            })  
        );


        ILLabel skipItemVoiding = w.DefineLabel();
        // go before foreach line
        w.MatchRelaxed(
            x => x.MatchLdloca(4) && w.SetCurrentTo(x),
            x => x.MatchCall(out _),
            x => x.MatchStloc(5)
        ).ThrowIfFailure()
        .InsertBeforeCurrentStealLabels(
          w.Create(OpCodes.Br, skipItemVoiding)  
        );

        // go to the end of the foreach block
        w.MatchRelaxed(
            x => x.MatchCallvirt<IDisposable>("Dispose"),
            x => x.MatchEndfinally() && w.SetCurrentTo(x)
        ).ThrowIfFailure()
        .MarkLabelToCurrentNext(skipItemVoiding);
    }


    private static ReturnFlow CharacterBody_Start(CharacterBody self)
    {
        if (self == null || self.master == null)
        {
            return ReturnFlow.None;
        }
        if (!_riVoidDiosInfoTable.TryGetValue(self.master, out var riVoidDiosInfo))
        {
            Log.Debug($"No void dios info was found for CharacterMaster {self.master}!");
            return ReturnFlow.None;
        }
        if (riVoidDiosInfo.NameOfSceneFirstRevivedIn == SceneManager.GetActiveScene().name)
        {
            return ReturnFlow.None;
        }


        Log.Info($"Respawning {self} as {riVoidDiosInfo.OriginalBodyPrefab} because of void dios.");
        self.master.bodyPrefab = riVoidDiosInfo.OriginalBodyPrefab;
        _riVoidDiosInfoTable.Remove(self.master);
        if (self.master.inventory != null)
        {
            if (self.master.inventory.GetItemCountEffective(_cutHpItemDef.itemIndex) > 0)
            {
                Log.Info($"Removing a CutHp from {self.name} due to starting in a new stage after being revived by a void dios in the previous stage");
                self.master.inventory.RemoveItemPermanent(_cutHpItemDef.itemIndex);
            }
            else
            {
                Log.Warning("CutHP count was less than 1 when there should've been 1! Something might've gone wrong?");
            }
        }
        else
        {
            Log.Error("SELF MASTER INVENTORY IN CharacterBody_Start WAS NULL!!!!! NOT GOOOD!!!!! YOU'RE STUCK WITH A CUTHP ITEM NOW!!!");
        }


        self.master.Respawn(self.master.bodyPrefab.name);
        return ReturnFlow.SkipOriginal;
    }


    // from DestroyedClone's TransformingAIFix/TransformingFix
    private static void ReplaceAISkillDrivers(CharacterMaster characterMaster, BaseAI baseAI, GameObject newCharacterMasterPrefab)
    {
        //Chat.AddMessage($"{characterMaster.name} has transformed into {newCharacterMasterPrefab.name}");
        foreach (var skillDriver in characterMaster.GetComponents<AISkillDriver>())
        {
            Destroy(skillDriver);
        }
        AISkillDriver[] listOfPrefabDrivers = newCharacterMasterPrefab.GetComponents<AISkillDriver>();
        List<AISkillDriver> newSkillDrivers = [];

        foreach (var skillDriver in listOfPrefabDrivers)
        {
            var newDriver = characterMaster.gameObject.AddComponent<AISkillDriver>();
            newDriver.activationRequiresAimConfirmation = skillDriver.activationRequiresAimConfirmation;
            newDriver.activationRequiresAimTargetLoS = skillDriver.activationRequiresAimTargetLoS;
            newDriver.activationRequiresTargetLoS = skillDriver.activationRequiresTargetLoS;
            newDriver.aimType = skillDriver.aimType;
            newDriver.buttonPressType = skillDriver.buttonPressType;
            newDriver.customName = skillDriver.customName;
            newDriver.driverUpdateTimerOverride = skillDriver.driverUpdateTimerOverride;
            newDriver.ignoreNodeGraph = skillDriver.ignoreNodeGraph;
            newDriver.maxDistance = skillDriver.maxDistance;
            newDriver.maxTargetHealthFraction = skillDriver.maxTargetHealthFraction;
            newDriver.maxUserHealthFraction = skillDriver.maxUserHealthFraction;
            newDriver.minDistance = skillDriver.minDistance;
            newDriver.minTargetHealthFraction = skillDriver.minTargetHealthFraction;
            newDriver.minUserHealthFraction = skillDriver.minUserHealthFraction;
            newDriver.moveInputScale = skillDriver.moveInputScale;
            newDriver.movementType = skillDriver.movementType;
            newDriver.moveTargetType = skillDriver.moveTargetType;
            newDriver.nextHighPriorityOverride = skillDriver.nextHighPriorityOverride;
            newDriver.noRepeat = skillDriver.noRepeat;
            newDriver.requiredSkill = skillDriver.requiredSkill;
            newDriver.requireEquipmentReady = skillDriver.requireEquipmentReady;
            newDriver.requireSkillReady = skillDriver.requireSkillReady;
            newDriver.resetCurrentEnemyOnNextDriverSelection = skillDriver.resetCurrentEnemyOnNextDriverSelection;
            newDriver.selectionRequiresAimTarget = skillDriver.selectionRequiresAimTarget;
            newDriver.selectionRequiresOnGround = skillDriver.selectionRequiresOnGround;
            newDriver.selectionRequiresTargetLoS = skillDriver.selectionRequiresTargetLoS;
            newDriver.shouldFireEquipment = skillDriver.shouldFireEquipment;
            newDriver.shouldSprint = skillDriver.shouldSprint;
            newDriver.buttonPressType = skillDriver.buttonPressType;
            newDriver.skillSlot = skillDriver.skillSlot;
            newDriver.name = $"{skillDriver.name}(Clone)"; // keeping things the same as if it was spawned in
            newSkillDrivers.Add(newDriver);
        }
        AISkillDriver[] array = [.. newSkillDrivers];
        baseAI.skillDrivers = array;

        EntityStateMachine esm = characterMaster.GetComponent<EntityStateMachine>();
        EntityStateMachine customESM = newCharacterMasterPrefab.GetComponent<EntityStateMachine>();
        esm.customName = customESM.customName;
        esm.initialStateType = customESM.initialStateType;
        esm.mainStateType = customESM.mainStateType;
        esm.nextState = customESM.nextState;
    }
}