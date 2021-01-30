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
            return role switch
            {
                JobRole.Tank => new Job[] { Job.GLA, Job.MRD, Job.PLD, Job.WAR, Job.DRK, Job.GNB },
                JobRole.Heal => new Job[] { Job.CNJ, Job.AST, Job.WHM, Job.SCH },
                JobRole.Melee => new Job[] { Job.PGL, Job.LNC, Job.MNK, Job.DRG, Job.ROG, Job.NIN, Job.SAM },
                JobRole.Ranged => new Job[] { Job.ARC, Job.BRD, Job.MCH, Job.DNC },
                JobRole.Magical => new Job[] { Job.THM, Job.BLM, Job.ACN, Job.SMN, Job.RDM, Job.BLU },
                JobRole.Crafter => new Job[] { Job.CRP, Job.BSM, Job.ARM, Job.GSM, Job.LTW, Job.WVR, Job.ALC, Job.CUL },
                JobRole.Gatherer => new Job[] { Job.MIN, Job.BTN, Job.FSH },
                _ => throw new ArgumentException($"Unknown jobRoleID {(int)role}"),
            };
        }
    }
}
