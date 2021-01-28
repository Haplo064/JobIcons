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
        public bool ShowName { get; set; } = false;
        public bool ShowTitle { get; set; } = false;
        public bool ShowFcName { get; set; } = false;
    }
}
