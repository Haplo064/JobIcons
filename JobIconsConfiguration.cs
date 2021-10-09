using Dalamud.Configuration;
using System;

namespace JobIcons
{
    public class JobIconsConfiguration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool Enabled { get; set; } = true;

        public float Scale { get; set; } = 1;

        public string TankIconSetName { get; set; } = "Glowing";
        public string HealIconSetName { get; set; } = "Glowing";
        public string MeleeIconSetName { get; set; } = "Glowing";
        public string RangedIconSetName { get; set; } = "Glowing";
        public string MagicalIconSetName { get; set; } = "Glowing";
        public string CraftingIconSetName { get; set; } = "Glowing";
        public string GatheringIconSetName { get; set; } = "Glowing";

        public int[] CustomIconSet1 { get; set; } = new int[Enum.GetValues(typeof(Job)).Length];
        public int[] CustomIconSet2 { get; set; } = new int[Enum.GetValues(typeof(Job)).Length];

        public short XAdjust { get; set; } = -13;
        public short YAdjust { get; set; } = 55;

        public bool SelfIcon { get; set; } = false;
        public bool PartyIcons { get; set; } = false;
        public bool AllianceIcons { get; set; } = false;
        public bool EveryoneElseIcons { get; set; } = false;

        public bool LocationAdjust { get; set; } = false;
        public bool ShowIcon { get; set; } = false;
        public bool ShowName { get; set; } = false;
        public bool JobName { get; set; } = false;
        public bool ShowTitle { get; set; } = false;
        public bool ShowFcName { get; set; } = false;

        internal IconSet GetIconSet(uint jobID)
        {
            var job = (Job)jobID;
            var jobRole = job.GetRole();
            return jobRole switch
            {
                JobRole.Tank => IconSet.Get(TankIconSetName),
                JobRole.Heal => IconSet.Get(HealIconSetName),
                JobRole.Melee => IconSet.Get(MeleeIconSetName),
                JobRole.Ranged => IconSet.Get(RangedIconSetName),
                JobRole.Magical => IconSet.Get(MagicalIconSetName),
                JobRole.Crafter => IconSet.Get(CraftingIconSetName),
                JobRole.Gatherer => IconSet.Get(GatheringIconSetName),
                _ => throw new ArgumentException($"Unknown jobID {(int)job}"),
            };
        }

        internal int GetIconID(uint jobID)
        {
            return GetIconSet(jobID).GetIconID(jobID);
        }
    }
}
