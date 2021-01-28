using System;

namespace JobIcons
{
    internal enum JobRole : uint
    {
        Tank = 1,
        Heal = 2,
        Melee = 3,
        Ranged = 4,
        Magical = 5,
        Crafter = 6,
        Gatherer = 7,
    }

    internal static class JobRoleExtensions
    {
        public static Job[] GetJobs(this JobRole role)
        {
            switch (role)
            {
                case JobRole.Tank: return new Job[] { Job.GLA, Job.MRD, Job.PLD, Job.WAR, Job.DRK, Job.GNB };
                case JobRole.Heal: return new Job[] { Job.CNJ, Job.AST, Job.WHM, Job.SCH };
                case JobRole.Melee: return new Job[] { Job.PGL, Job.LNC, Job.MNK, Job.DRG, Job.ROG, Job.NIN, Job.SAM };
                case JobRole.Ranged: return new Job[] { Job.ARC, Job.BRD, Job.MCH, Job.DNC };
                case JobRole.Magical: return new Job[] { Job.THM, Job.BLM, Job.ACN, Job.SMN, Job.RDM, Job.BLU };
                case JobRole.Crafter: return new Job[] { Job.CRP, Job.BSM, Job.ARM, Job.GSM, Job.LTW, Job.WVR, Job.ALC, Job.CUL };
                case JobRole.Gatherer: return new Job[] { Job.MIN, Job.BTN, Job.FSH };
                default: throw new ArgumentException($"Unknown jobRoleID {(int)role}");
            }
        }
    }
}
