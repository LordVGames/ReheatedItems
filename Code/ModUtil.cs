using Mono.Cecil.Cil;
using MonoDetour.Cil;
using MonoDetour.Cil.Analysis;
using MonoMod.Cil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using R2API;
using RoR2;
using RoR2.Projectile;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
namespace ReheatedItems;


internal static class ModUtil
{
    internal static void LogILInstructions(this ILWeaver w)
    {
        Log.Warning(w.Method.Body.CreateInformationalSnapshotJIT().ToStringWithAnnotations());
    }


    internal static ILWeaverResult MatchNextRelaxed(this ILWeaver w, params Predicate<Instruction>[] predicates)
    {
        bool foundNextMatch = false;
        int oldWeaverOffset = w.Current.Offset;
        Instruction instructionToStayOn = null;

        ILWeaverResult matchResult = w.MatchMultipleRelaxed(
            onMatch: w2 =>
            {
                //Log.Debug($"w.Current.Offset {w.Current.Offset}");
                //Log.Debug($"w2.Current.Offset {w2.Current.Offset}");
                //Log.Debug($"w.Current {w.Current}");
                //Log.Debug($"w2.Current {w2.Current}");
                if (w2.Current.Offset > oldWeaverOffset && !foundNextMatch)
                {
                    //Log.Debug("FOUND");
                    foundNextMatch = true;
                    instructionToStayOn = w2.Current;
                }
            },
            predicates
        );
        if (!foundNextMatch)
        {
            return new ILWeaverResult(w, w.GetMatchToNextRelaxedErrorMessage);
        }

        w.SetCurrentTo(instructionToStayOn); // idk, just in case
        return matchResult;
    }
    private static string GetMatchToNextRelaxedErrorMessage(this ILWeaver w)
    {
        // this is stupid (i think?)
        StringBuilder sb = new();
        sb.Append(w.Method.Body.CreateInformationalSnapshotJIT().ToStringWithAnnotations());
        sb.AppendFormat($"\nLast Weaver Position: {w.Current}");
        sb.AppendFormat($"\nPrevious: {w.Previous}");
        sb.AppendFormat($"\nNext: {w.Next}");
        sb.AppendLine("\n\n! MatchNextRelaxed FAILED !\nA match was found, but it was not further ahead than the weaver's position!");
        return sb.ToString();
    }


    internal static void SkipNext2FireProjectiles(ILWeaver w)
    {
        ILLabel skipOverBad = w.DefineLabel();


        // go to start of first one
        ILWeaverResult firstMatch = w.MatchNextRelaxed(
            x => x.MatchCallOrCallvirt<ProjectileManager>("get_instance") && w.SetCurrentTo(x)
        );
        w.InsertBeforeCurrent(
            w.Create(OpCodes.Br, skipOverBad)
        );


        // go to start of second one to position for end of second one
        w.MatchNextRelaxed(
            x => x.MatchCallOrCallvirt<ProjectileManager>("get_instance") && w.SetCurrentTo(x)
        ).ThrowIfFailure();


        // go to end of second one
        ILWeaverResult match = w.MatchNextRelaxed(
            x => x.MatchCallOrCallvirt<ProjectileManager>("FireProjectile") && w.SetCurrentTo(x)
        );
        if (!match.IsValid)
        {
            Log.Warning("FireProjectile WAS NOT VALID????");
            w.MatchNextRelaxed(
                x => x.MatchCallOrCallvirt<ProjectileManager>("FireProjectileWithoutDamageType") && w.SetCurrentTo(x)
            ).ThrowIfFailure();
        }

        
        w.MarkLabelToCurrentNext(skipOverBad);
    }


    internal static bool IsDamageReportNull(DamageReport damageReport)
    {
        return damageReport.victim == null
            || damageReport.victimBody == null
            || damageReport.attacker == null
            || damageReport.attackerBody == null
            || damageReport.attackerBody.inventory == null
            || damageReport.attackerMaster == null;
    }


    internal static bool IsDamageInfoNull(DamageInfo damageInfo)
    {
        return damageInfo.attacker == null
            || damageInfo.inflictor == null;
    }


    internal static DamageInfo CreateNewDamageInfoFromDamageReport(DamageReport damageReport)
    {
        // i hate making deep copies
        return new()
        {
            attacker = damageReport.damageInfo.attacker,
            canRejectForce = damageReport.damageInfo.canRejectForce,
            crit = damageReport.damageInfo.crit,
            damageType = damageReport.damageInfo.damageType,
            delayedDamageSecondHalf = damageReport.damageInfo.delayedDamageSecondHalf,
            dotIndex = damageReport.damageInfo.dotIndex,
            firstHitOfDelayedDamageSecondHalf = damageReport.damageInfo.firstHitOfDelayedDamageSecondHalf,
            inflictedHurtbox = damageReport.damageInfo.inflictedHurtbox,
            inflictor = damageReport.damageInfo.inflictor,
            physForceFlags = damageReport.damageInfo.physForceFlags,
            position = damageReport.damageInfo.position,
            rejected = damageReport.damageInfo.rejected,
            force = damageReport.damageInfo.force,
            procCoefficient = 1,
            procChainMask = damageReport.damageInfo.procChainMask,
            damage = damageReport.damageInfo.damage,
            damageColorIndex = DamageColorIndex.Item
        };
    }


    internal static readonly DamageTypeCombo GenericEquipmentDamageType = new()
    {
        damageType = DamageType.Generic,
        damageTypeExtended = DamageTypeExtended.Generic,
        damageSource = DamageSource.Equipment
    };


    internal static void AddBuffOnServer(this CharacterBody characterBody, BuffDef buffDef)
    {
        if (NetworkServer.active)
        {
            characterBody.AddBuff(buffDef);
        }
    }


    internal static void AddTimedBuffOnServer(this CharacterBody characterBody, BuffDef buffDef, float duration)
    {
        if (NetworkServer.active)
        {
            characterBody.AddTimedBuff(buffDef, duration);
        }
    }


    internal static void RemoveBuffOnServer(this CharacterBody characterBody, BuffDef buffDef)
    {
        if (NetworkServer.active)
        {
            characterBody.RemoveBuff(buffDef);
        }
    }
}