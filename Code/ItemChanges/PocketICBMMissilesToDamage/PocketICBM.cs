using Mono.Cecil.Cil;
using MonoDetour;
using MonoDetour.Cil;
using MonoDetour.DetourTypes;
using MonoDetour.HookGen;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace ReheatedItems.ItemChanges.PocketICBMMissilesToDamage;


[MonoDetourTargets(typeof(GlobalEventManager))]
[MonoDetourTargets(typeof(MissileUtils), GenerateControlFlowVariants = true)]
public static class PocketICBM
{
    private const float _initialMult = 3f;
    private const float _stackMult = 1.5f;

    [MonoDetourHookInitialize]
    private static void Setup()
    {
        if (!ConfigOptions.PocketICBM.EnableEdit.Value)
        {
            return;
        }


        ModLanguage.LangFilesToLoad.Add("PocketICBM");
        if (ConfigOptions.PocketICBM.ChangeGenericMissileEffect.Value)
        {
            Mdh.RoR2.MissileUtils.GetMoreMissileDamageMultiplier.ControlFlowPrefix(EditICBMDamageMult);
            Mdh.RoR2.MissileUtils.FireMissile_UnityEngine_Vector3_RoR2_CharacterBody_RoR2_ProcChainMask_UnityEngine_GameObject_System_Single_System_Boolean_UnityEngine_GameObject_RoR2_DamageColorIndex_UnityEngine_Vector3_System_Single_System_Boolean.ILHook(ChangeGenericICBMEffect);
        }
    }

    private static ReturnFlow EditICBMDamageMult(ref int moreMissileCount, ref float returnValue)
    {
        returnValue = GetICBMDamageMult(moreMissileCount);
        return ReturnFlow.SkipOriginal;
    }

    private static void ChangeGenericICBMEffect(ILManipulationInfo info)
    {
        ILWeaver w = new(info);
        ILLabel skipToEnd = w.DefineLabel();

        // going to start of line:
        // if (num > 0)
        // (after a FireProjectile line)
        w.MatchRelaxed(
            x => x.MatchLdcI4(0),
            x => x.MatchBle(out skipToEnd) && w.SetCurrentTo(x)
        ).ThrowIfFailure()
        .InsertAfterCurrent(
            w.Create(OpCodes.Br, skipToEnd)
        );
    }



    public static float GetICBMDamageMultForCharacterBody(CharacterBody characterBody)
    {
        int icbmCount = 0;
        if (characterBody != null && characterBody.inventory != null)
        {
            icbmCount = characterBody.inventory.GetItemCountEffective(DLC1Content.Items.MoreMissile);
        }

        return GetICBMDamageMult(icbmCount);
    }

    public static float GetICBMDamageMult(int icbmCount)
    {
        if (icbmCount > 0)
        {
            return _initialMult + (_stackMult * (icbmCount - 1));
        }
        else
        {
            return 1;
        }
    }
}