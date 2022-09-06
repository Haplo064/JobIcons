using System;
using System.Collections.Generic;

namespace JobIcons2
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
        public static IEnumerable<Job> GetJobs(this JobRole role)
        {
            return role switch
            {
                JobRole.Tank => new[] { Job.GLA, Job.MRD, Job.PLD, Job.WAR, Job.DRK, Job.GNB },
                JobRole.Heal => new[] { Job.CNJ, Job.AST, Job.WHM, Job.SCH },
                JobRole.Melee => new[] { Job.PGL, Job.LNC, Job.MNK, Job.DRG, Job.ROG, Job.NIN, Job.SAM },
                JobRole.Ranged => new[] { Job.ARC, Job.BRD, Job.MCH, Job.DNC },
                JobRole.Magical => new[] { Job.THM, Job.BLM, Job.ACN, Job.SMN, Job.RDM, Job.BLU },
                JobRole.Crafter => new[] { Job.CRP, Job.BSM, Job.ARM, Job.GSM, Job.LTW, Job.WVR, Job.ALC, Job.CUL },
                JobRole.Gatherer => new[] { Job.MIN, Job.BTN, Job.FSH },
                _ => throw new ArgumentException($"Unknown jobRoleID {(int)role}"),
            };
        }
    }
}
