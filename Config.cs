using Dalamud.Configuration;

namespace JobIcons
{
    public class Config : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public bool Enabled { get; set; } = true;
        public float Scale { get; set; } = 1f;
        public int[] Role { get; set; } = { 0, 0, 0, 0, 0 };
        public int XAdjust { get; set; } = -13;
        public int YAdjust { get; set; } = 55;
        public bool ShowName { get; set; } = true;
        public bool ShowTitle { get; set; } = true;
        public bool ShowFC { get; set; } = true;
    }
}