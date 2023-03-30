﻿using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using System;
using UnityEngine;

namespace RimArchive;

//不会画渐变的扩张特效，摆了
public class CompInvadePillar : ThingComp
{
    private const float _radiusAdd = 2.5f;
    private float _currentRadius;
    private int _interval;
    private int _ticks;
    private CompMoteEmitterIncreasingSize cachedEmitter;
    //private Mote mote;
    public CompProperties_InvadePillar Props => (CompProperties_InvadePillar)props;

    private CompMoteEmitterIncreasingSize Emitter => cachedEmitter ??= this.parent.GetComp<CompMoteEmitterIncreasingSize>();

    public float Radius => _currentRadius;
    public override void CompTick()
    {
        base.CompTick();
        ++_ticks;
        (from pawn in parent.Map.mapPawns.AllPawnsSpawned
         where pawn.Position.InHorDistOf(parent.Position, _currentRadius)
         select pawn).Do(delegate (Pawn x)
         {
             foreach (HediffDef hediff in Props.applyHediffs)
             {
                 if (x.health.hediffSet.HasHediff(hediff) && x.health.hediffSet.GetFirstHediffOfDef(hediff).ageTicks % 600 == 0)
                 {
                     ++x.health.hediffSet.GetFirstHediffOfDef(hediff).Severity;
                 }
                 else
                 {
                     x.health.AddHediff(hediff);
                 }
             }
         });
        //emmm...虽说IsHashIntervalTick挺好用但貌似没有“重置Tick数”的功能
        if (_ticks % _interval == 0)
        {
            _ticks = 0;
            _currentRadius += _radiusAdd;
            Emitter.Notify_RadiusChanged();
        }
    }

    //构造函数被调用的时候还没有props
    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);
        _currentRadius = Props.InitialRadius;
        _interval = Props.upgradeInterval * GenTicks.TicksPerRealSecond;
    }

    public override string CompInspectStringExtra()
    {
        string str = base.CompInspectStringExtra();
        str += "\n" + "Current Radius:" + _currentRadius.ToString("F2");
        return str;
    }

    public void Notify_Stunned()
    {
        _currentRadius -= _radiusAdd;
        _ticks = 0;
        Emitter.Notify_RadiusChanged();
    }
}

