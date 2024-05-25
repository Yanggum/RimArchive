using System;
using System.Text;
using Verse;

namespace RimArchive
{
    internal static class DebugMessage
    {
        internal static void DbgMsg(string message)
        {
            Log.Message("[RimArchive] " + message);
        }

        internal static void DbgWrn(string message)
        {
            Log.Warning("[RimArchive] " + message);
        }

        internal static void DbgErr(string message)
        {
            Log.Error("[RimArchive] " + message);
        }
    }

    public enum DebugActionType
    {
        Action,
        ToolMap
    }

    public enum AllowedGameStates
    {
        PlayingOnMap,
        Entry
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class DebugActionAttribute : Attribute
    {
        public string Category { get; }
        public string Name { get; }
        public bool RequiresRoyalty { get; }
        public bool RequiresIdeology { get; }
        public bool RequiresBiotech { get; }
        public int DisplayPriority { get; }
        public bool HideInSubMenu { get; }

        public DebugActionAttribute(
            string name = null,
            bool requiresRoyalty = false,
            bool requiresIdeology = false,
            bool requiresBiotech = false,
            int displayPriority = 0,
            bool hideInSubMenu = false)
        {
            Category = "RimArchive";
            Name = name;
            RequiresRoyalty = requiresRoyalty;
            RequiresIdeology = requiresIdeology;
            RequiresBiotech = requiresBiotech;
            DisplayPriority = displayPriority;
            HideInSubMenu = hideInSubMenu;
        }
    }

    public static class DebugTools
    {
        [DebugAction(name: "ResetRaidData")]
        private static void ResetRaidData() => RimArchive.RaidManager.DebugResetRaid();

        [DebugAction(name: "ReShuffleRaid")]
        private static void ReShuffleRaid() => RimArchive.RaidManager.DebugRandomRaid();

        /*[DebugAction("OutputCurrentHediff", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void HediffOutput()
        {
            if (!RimArchiveMain.HediffGen.stages.NullOrEmpty())
            {
                StringBuilder sb = new StringBuilder();
                foreach (HediffStage stage in RimArchiveMain.HediffGen.stages)
                {
                    sb.AppendLine(stage.minSeverity.ToString());
                    foreach (StatModifier factor in stage.statFactors)
                    {
                        sb.AppendLine(factor.stat.defName);
                        sb.AppendLine(factor.value.ToString());
                    }
                    sb.AppendLine();
                }
                Log.Message(sb.ToString());
            }
        }*/
    }
}
