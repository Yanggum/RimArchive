using UnityEngine;
using AlienRace;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace RimArchive
{
    public class ScenPart_ForcedRace : ScenPart_PawnModifier
    {
        public ThingDef race;

        protected override void ModifyNewPawn(Pawn p)
        {
            if (p.def != this.race)
                p.def = this.race;
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            race ??= PossibleRaces().RandomElement();
            Rect scenPartRect = listing.GetScenPartRect(this, RowHeight * 3.0f + 31.0f);
            if (Widgets.ButtonText(scenPartRect.TopPartPixels(RowHeight), (string)race.LabelCap))
            {
                FloatMenuUtility.MakeMenu<ThingDef>(PossibleRaces(), x => (string)x.LabelCap, x => () => { this.race = x; });
            }
        }

        protected override void ModifyPawnPostGenerate(Pawn p, bool redressed)
        {
            p.story.hairDef = HairDefOf.Bald;
            p.style.beardDef = BeardDefOf.NoBeard;
            p.genes.SetXenotype((p.def as ThingDef_AlienRace).alienRace.raceRestriction.xenotypeList.First());
            p.genes.ClearXenogenes();
            foreach (var gene in p.genes.Xenotype.AllGenes)
                p.genes.AddGene(gene, false);
        }

        private static IEnumerable<ThingDef> PossibleRaces()
        {
            if (ModLister.HasActiveModWithName("Humanoid Alien Races") || ModsConfig.IsActive("erdelf.humanoidalienraces"))
                return DefDatabase<ThingDef_AlienRace>.AllDefs.Where(x => x.race?.Humanlike ?? false);
            return DefDatabase<ThingDef>.AllDefs.Where(x => x.race?.Humanlike ?? false);
        }
    }

    public class ScenPart_FixedStartingPawns : ScenPart_ConfigPage_ConfigureStartingPawnsBase
    {
        public new int pawnChoiceCount = 0;
        public int pawnCount = 3;
        [MustTranslate]
        public string customSummary;
        public List<PawnKindCount> possibleKindDefs = new List<PawnKindCount>();

        protected override int TotalPawnCount => possibleKindDefs.Sum(x => x.count);

        public override string Summary(Scenario scen) => customSummary ?? "ScenPart_StartWithCertainColonists".Translate();

        public IEnumerable<PawnKindDef> availableDefs => DefDatabase<PawnKindDef>.AllDefs.Where(x => x.RaceProps.Humanlike && x.defaultFactionType != null && x.defaultFactionType.isPlayer).Except(possibleKindDefs.Select(x => x.kindDef));

        protected override void GenerateStartingPawns()
        {
            var num = 0;
            StartingPawnUtility.ClearAllStartingPawns();
            for (int i = 0; i < possibleKindDefs.Count; i++)
            {
                StartingPawnUtility.SetGenerationRequest(TotalPawnCount, new PawnGenerationRequest(this.possibleKindDefs[i].kindDef));
                //StartingPawnUtility.StartingAndOptionalPawnGenerationRequests.Add(StartingPawnUtility.DefaultStartingPawnRequest);
                Pawn p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(possibleKindDefs[i].kindDef, Faction.OfPlayer, canGeneratePawnRelations: false, colonistRelationChanceFactor: 0f));
                if (possibleKindDefs[i].kindDef is StudentDef s)
                {
                    StudentGenerationUtility.PostGen(p, s, PawnGenerationContext.PlayerStarter);
                }
                if (possibleKindDefs[i].kindDef == PawnKindDefOfLocal.RA_PawnKindDef_Sensei)
                {
                    p.apparel.Wear(ThingMaker.MakeThing(ThingDefOf.Shittim_Chest_Apparel, GenStuff.RandomStuffFor(ThingDefOf.Shittim_Chest_Apparel)) as Apparel, false, false);
                }
                Find.GameInitData.startingAndOptionalPawns.Insert(i, p);
                StartingPawnUtility.GeneratePossessions(p);
                num++;
            }
            StartingPawnUtility.SetGenerationRequest(TotalPawnCount, new PawnGenerationRequest(possibleKindDefs.RandomElement().kindDef));
            while (num < TotalPawnCount && !StartingPawnUtility.WorkTypeRequirementsSatisfied())
            {
                StartingPawnUtility.AddNewPawn();
                num++;
            }
        }

        public override int GetHashCode()
        {
            int hashCode = base.GetHashCode();
            foreach (PawnKindCount kindCount in possibleKindDefs)
                hashCode ^= kindCount.GetHashCode();
            return hashCode;
        }

        public override void PostIdeoChosen()
        {
            Find.GameInitData.startingPawnCount = TotalPawnCount;
            Find.GameInitData.startingPawnsRequired = Find.GameInitData.startingAndOptionalPawns.Select(x => new PawnKindCount() { kindDef = x.kindDef }).ToList();
            if (ModsConfig.BiotechActive)
            {
                Current.Game.customXenotypeDatabase.customXenotypes.Clear();
                foreach (Ideo ideo in Find.IdeoManager.IdeosListForReading)
                {
                    foreach (Precept precept in ideo.PreceptsListForReading)
                    {
                        if (precept is Precept_Xenotype preceptXenotype && preceptXenotype.customXenotype != null && !Current.Game.customXenotypeDatabase.customXenotypes.Contains(preceptXenotype.customXenotype))
                            Current.Game.customXenotypeDatabase.customXenotypes.Add(preceptXenotype.customXenotype);
                    }
                }
            }
            if (ModsConfig.IdeologyActive && Faction.OfPlayerSilentFail?.ideos?.PrimaryIdeo != null)
            {
                foreach (Precept precept in Faction.OfPlayerSilentFail.ideos.PrimaryIdeo.PreceptsListForReading)
                {
                    if (precept.def.defaultDrugPolicyOverride != null)
                        Current.Game.drugPolicyDatabase.MakePolicyDefault(precept.def.defaultDrugPolicyOverride);
                }
            }
            GenerateStartingPawns();
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect rect = listing.GetScenPartRect(this, RowHeight * (TotalPawnCount + 1));
            for (int i = 0; i < possibleKindDefs.Count; i++)
            {
                PawnKindCount kindCount = possibleKindDefs[i];
                Rect menuRect = new Rect(rect) { xMax = rect.xMax - RowHeight };
                if (Widgets.ButtonText(menuRect, kindCount.kindDef.LabelCap))
                {
                    FloatMenuUtility.MakeMenu(availableDefs, x => (string)x.LabelCap, delegate (PawnKindDef x) { return () => kindCount.kindDef = x; });
                }
                if (Widgets.ButtonImage(new Rect(menuRect.xMax, menuRect.y, RowHeight, RowHeight), TexButton.Delete))
                {
                    possibleKindDefs.RemoveAt(i);
                    return;
                }
                rect.y += RowHeight;
            }
            if (Widgets.ButtonText(rect, "Add"))
            {
                FloatMenuUtility.MakeMenu(availableDefs, x => (string)x.LabelCap, delegate (PawnKindDef x) { return () => possibleKindDefs.Add(new PawnKindCount() { kindDef = x }); });
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref possibleKindDefs, "kindCounts", LookMode.Deep);
        }
    }
}
