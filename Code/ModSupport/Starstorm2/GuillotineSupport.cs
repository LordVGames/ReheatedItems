using RoR2;
using SS2;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
namespace ReheatedItems.ModSupport.Starstorm2;


internal static class GuillotineSupport
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void TrySetUltraEliteEffectsState(CharacterBody characterBody, bool effectState)
    {
        if (characterBody.inventory.GetItemCountEffective(SS2Content.Items.AffixUltra) < 1)
        {
            return;
        }


        // i am not removing the character size increase, for me that falls under "stat increases enemy gets from being elite"
        if (effectState)
        {
            characterBody.AddBuff(SS2Content.Buffs.bdUltra);
            characterBody.AddBuff(SS2Content.Buffs.bdUltraBuff);
        }
        else
        {
            characterBody.RemoveBuff(SS2Content.Buffs.bdUltra);
            characterBody.RemoveBuff(SS2Content.Buffs.bdUltraBuff);
        }
        Transform ultraWard = characterBody.transform.Find("UltraWard(Clone)");
        ultraWard?.gameObject.SetActive(effectState);
    }


    // ethereal buff gets removed properly, but not it's effects
    // and both don't get re-added
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void TrySetEtherealEliteEffectsState(CharacterBody characterBody, bool effectState)
    {
        if (characterBody.TryGetComponent<SS2.Equipments.AffixEthereal.BodyBehavior>(out var bodyBehavior))
        {
            bodyBehavior.enabled = effectState;
            if (effectState)
            {
                characterBody.AddTimedBuff(ItemChanges.Guillotine.Buffs.AllowEliteBuffReAdded.bdReAddEliteBuff, 0.1f);
                characterBody.AddBuff(SS2Content.Buffs.bdEthereal);
            }
        }
    }
}