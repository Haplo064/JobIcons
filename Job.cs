using System;

namespace JobIcons
{
    internal enum Job : uint
    {
        ADV = 0,
        GLA = 1,
        PGL = 2,
        MRD = 3,
        LNC = 4,
        ARC = 5,
        CNJ = 6,
        THM = 7,
        CRP = 8,
        BSM = 9,
        ARM = 10,
        GSM = 11,
        LTW = 12,
        WVR = 13,
        ALC = 14,
        CUL = 15,
        MIN = 16,
        BTN = 17,
        FSH = 18,
        PLD = 19,
        MNK = 20,
        WAR = 21,
        DRG = 22,
        BRD = 23,
        WHM = 24,
        BLM = 25,
        ACN = 26,
        SMN = 27,
        SCH = 28,
        ROG = 29,
        NIN = 30,
        MCH = 31,
        DRK = 32,
        AST = 33,
        SAM = 34,
        RDM = 35,
        BLU = 36,
        GNB = 37,
        DNC = 38,
    }

    internal static class JobExtensions
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0066:Convert switch statement to expression", Justification = "No, it looks dumb")]
        public static JobRole GetRole(this Job job)
        {
            switch (job)
            {
                case Job.GLA:
                case Job.MRD:
                case Job.PLD:
                case Job.WAR:
                case Job.DRK:
                case Job.GNB: return JobRole.Tank;
                case Job.CNJ:
                case Job.AST:
                case Job.WHM:
                case Job.SCH: return JobRole.Heal;
                case Job.PGL:
                case Job.LNC:
                case Job.MNK:
                case Job.DRG:
                case Job.ROG:
                case Job.NIN:
                case Job.SAM: return JobRole.Melee;
                case Job.ARC:
                case Job.BRD:
                case Job.MCH:
                case Job.DNC: return JobRole.Ranged;
                case Job.THM:
                case Job.BLM:
                case Job.ACN:
                case Job.SMN:
                case Job.RDM:
                case Job.BLU: return JobRole.Magical;
                case Job.CRP:
                case Job.BSM:
                case Job.ARM:
                case Job.GSM:
                case Job.LTW:
                case Job.WVR:
                case Job.ALC:
                case Job.CUL: return JobRole.Crafter;
                case Job.MIN:
                case Job.BTN:
                case Job.FSH: return JobRole.Gatherer;
                default: throw new ArgumentException($"Unknown jobID {(int)job}");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0066:Convert switch statement to expression", Justification = "No, it looks dumb")]
        public static string GetName(this Job job)
        {
            switch ((uint)job)
            {
                case 0: return "";
                case 1: return "剑术师";
                case 2: return "格斗家";
                case 3: return "斧术师";
                case 4: return "枪术师";
                case 5: return "弓箭手";
                case 6: return "幻术师";
                case 7: return "咒术师";
                case 8: return "刻木匠";
                case 9: return "锻铁匠";
                case 10: return "铸甲匠";
                case 11: return "雕金匠";
                case 12: return "制革匠";
                case 13: return "裁衣匠";
                case 14: return "炼金术士";
                case 15: return "烹调师";
                case 16: return "采矿工";
                case 17: return "园艺工";
                case 18: return "捕鱼人";
                case 19: return "骑士";
                case 20: return "武僧";
                case 21: return "战士";
                case 22: return "龙骑士";
                case 23: return "诗人";
                case 24: return "白魔法师";
                case 25: return "黑魔法师";
                case 26: return "秘术师";
                case 27: return "召唤师";
                case 28: return "学者";
                case 29: return "双剑师";
                case 30: return "忍者";
                case 31: return "机工士";
                case 32: return "暗黑骑士";
                case 33: return "占星术士";
                case 34: return "武士";
                case 35: return "赤魔法师";
                case 36: return "青魔法师";
                case 37: return "绝枪战士";
                case 38: return "舞者";
                default: throw new ArgumentException($"Unknown jobID {(int)job}");
            }
        }
    }
}
