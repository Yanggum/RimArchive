﻿using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace RimArchive;


//全部重写。
//周期参考每次总力设为7天
//
public class BossGroupWorker : BossgroupWorker
{
    new public const int TimeBetweenAllBossgroups = 420000;
    new RaidDef def;
    public override AcceptanceReport CanResolve(Pawn caller)
    {
        int num = Find.TickManager.TicksGame - RimArchiveMain.RaidManager.lastRaidCalled;
        if (num < TimeBetweenAllBossgroups)
        {
            return (AcceptanceReport)"BossgroupAvailableIn".Translate((NamedArgument)(120000 - num).ToStringTicksToPeriod());
        }
        PawnKindDef pendingBossgroup = CallBossgroupUtility.GetPendingBossgroup();
        return pendingBossgroup != null ? (AcceptanceReport)"BossgroupIncoming".Translate((NamedArgument)pendingBossgroup.label) : (AcceptanceReport)true;
    }

    public override AcceptanceReport ShouldSummonNow(Map map)
    {
        return (AcceptanceReport)true;
    }

    public override void Resolve(Map map, int wave)
    {
        RimArchiveMain.RaidManager.Notify_BossgroupCalled(this.def);
        Slate vars = new Slate();
        vars.Set<RaidDef>("bossgroup", this.def);
        vars.Set<Map>(nameof(map), map);
        vars.Set<ThingDef>("reward", this.def.rewardDef);
        vars.Set<int>(nameof(wave), wave);
        QuestUtility.GenerateQuestAndMakeAvailable(this.def.quest, vars);
    }
}
