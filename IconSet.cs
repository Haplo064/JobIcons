using System;
using System.Collections.Generic;
using System.Linq;

namespace JobIcons
{
    internal class IconSet
    {
        #region static

        private static JobIconsPlugin plugin;

        public static void Initialize(JobIconsPlugin plugin)
        {
            IconSet.plugin = plugin;

            Add("Gold", new int[] {
                62001, 62002, 62003, 62004, 62005, 62006, 62007, 62008, 62009, 62010,
                62011, 62012, 62013, 62014, 62015, 62016, 62017, 62018, 62019, 62020,
                62021, 62022, 62023, 62024, 62025, 62026, 62027, 62028, 62029, 62030,
                62031, 62032, 62033, 62034, 62035, 62036, 62037, 62038, 62039, 62040 });
            Add("Framed", new int[] {
                62101, 62102, 62103, 62104, 62105, 62106, 62107, 62108, 62109, 62110,
                62111, 62112, 62113, 62114, 62115, 62116, 62117, 62118, 62119, 62120,
                62121, 62122, 62123, 62124, 62125, 62126, 62127, 62128, 62129, 62130,
                62131, 62132, 62133, 62134, 62135, 62136, 62137, 62138, 62139, 62140 });
            Add("Glowing", new int[] {
                62301, 62302, 62303, 62304, 62305, 62306, 62307, 62310, 62311, 62312,
                62313, 62314, 62315, 62316, 62317, 62318, 62319, 62320, 62401, 62402,
                62403, 62404, 62405, 62406, 62407, 62308, 62408, 62409, 62309, 62410,
                62411, 62412, 62413, 62414, 62415, 62416, 62417, 62418, 62419, 62420 });
            Add("Grey", new int[] {
                91022, 91023, 91024, 91025, 91026, 91028, 91029, 91031, 91032, 91033,
                91034, 91035, 91036, 91037, 91038, 91039, 91040, 91041, 91079, 91080,
                91081, 91082, 91083, 91084, 91085, 91030, 91086, 91087, 91121, 91122,
                91125, 91123, 91124, 91127, 91128, 91129, 91130, 91131, 91132, 91133 }, 2);
            Add("Black", new int[] {
                91522, 91523, 91524, 91525, 91526, 91528, 91529, 91531, 91532, 91533,
                91534, 91535, 91536, 91537, 91538, 91539, 91540, 91541, 91579, 91580,
                91581, 91582, 91583, 91584, 91585, 91530, 91586, 91587, 91621, 91622,
                91625, 91623, 91624, 91627, 91628, 91629, 91630, 91631, 91632, 91633 }, 2);
            Add("Yellow", new int[] {
                92022, 92023, 92024, 92025, 92026, 92028, 92029, 92031, 92032, 92033,
                92034, 92035, 92036, 92037, 92038, 92039, 92040, 92041, 92079, 92080,
                92081, 92082, 92083, 92084, 92085, 92030, 92086, 92087, 92121, 92122,
                92125, 92123, 92124, 92127, 92128, 92129, 92130, 92131, 92132, 92133 }, 2);
            Add("Orange", new int[] {
                92522, 92523, 92524, 92525, 92526, 92528, 92529, 92531, 92532, 92533,
                92534, 92535, 92536, 92537, 92538, 92539, 92540, 92541, 92579, 92580,
                92581, 92582, 92583, 92584, 92585, 92530, 92586, 92587, 92621, 92622,
                92625, 92623, 92624, 92627, 92628, 92629, 92630, 92631, 92632, 92633 }, 2);
            Add("Red", new int[] {
                93022, 93023, 93024, 93025, 93026, 93028, 93029, 93031, 93032, 93033,
                93034, 93035, 93036, 93037, 93038, 93039, 93040, 93041, 93079, 93080,
                93081, 93082, 93083, 93084, 93085, 93030, 93086, 93087, 93121, 93122,
                93125, 93123, 93124, 93127, 93128, 93129, 93130, 93131, 93132, 93133 }, 2);
            Add("Purple", new int[] {
                93522, 93523, 93524, 93525, 93526, 93528, 93529, 93531, 93532, 93533,
                93534, 93535, 93536, 93537, 93538, 93539, 93540, 93541, 93579, 93580,
                93581, 93582, 93583, 93584, 93585, 93530, 93586, 93587, 93621, 93622,
                93625, 93623, 93624, 93627, 93628, 93629, 93630, 93631, 93632, 93633 }, 2);
            Add("Blue", new int[] {
                94022, 94023, 94024, 94025, 94026, 94028, 94029, 94031, 94032, 94033,
                94034, 94035, 94036, 94037, 94038, 94039, 94040, 94041, 94079, 94080,
                94081, 94082, 94083, 94084, 94085, 94030, 94086, 94087, 94121, 94122,
                94125, 94123, 94124, 94127, 94128, 94129, 94130, 94131, 94132, 94133 }, 2);
            Add("Green", new int[] {
                94522, 94523, 94524, 94525, 94526, 94528, 94529, 94531, 94532, 94533,
                94534, 94535, 94536, 94537, 94538, 94539, 94540, 94541, 94579, 94580,
                94581, 94582, 94583, 94584, 94585, 94530, 94586, 94587, 94621, 94622,
                94625, 94623, 94624, 94627, 94628, 94629, 94630, 94631, 94632, 94633 }, 2);
            Add("Role", new int[] {
                62581, 62584, 62581, 62584, 62586, 62582, 62502, 62502, 62503, 62504,
                62505, 62506, 62507, 62508, 62509, 62510, 62511, 62512, 62581, 62584,
                62581, 62584, 62586, 62582, 62587, 62587, 62587, 62582, 62584, 62584,
                62586, 62581, 62582, 62584, 62587, 62587, 62581, 62586, 62584, 62582 });
            Add("Custom1", new Func<Job, int>((job) => plugin.Configuration.CustomIconSet1[(int)job]));
            Add("Custom2", new Func<Job, int>((job) => plugin.Configuration.CustomIconSet2[(int)job]));
        }

        private const float DEFAULT_SCALE_MULTIPLIER = 1;

        public static IconSet Get(string name) => IconSets[name];

        public static string[] Names => IconSets.Keys.ToArray();

        public static void Add(string name, int[] icons, float scaleMultiplier = DEFAULT_SCALE_MULTIPLIER)
        {
            IconSets[name] = new IconSet(name, icons, scaleMultiplier);
        }

        public static void Add(string name, Func<Job, int> iconMethod, float scaleMultiplier = DEFAULT_SCALE_MULTIPLIER)
        {
            IconSets[name] = new IconSet(name, iconMethod, scaleMultiplier);
        }

        private static readonly Dictionary<string, IconSet> IconSets = new Dictionary<string, IconSet>();

        #endregion

        public string Name { get; private set; }
        public float ScaleMultiplier { get; private set; }

        private readonly int[] Icons;
        private readonly Func<Job, int> IconMethod;

        private IconSet(string name, int[] icons, float scaleMultiplier)
        {
            var numJobs = Enum.GetNames(typeof(Job)).Length - 1;
            if (icons.Length != numJobs)
                throw new ArgumentException($"IconSet was provided {icons.Length} icons, expected {numJobs}");

            Name = name;
            Icons = icons;
            ScaleMultiplier = scaleMultiplier;
        }

        private IconSet(string name, Func<Job, int> iconMethod, float scaleMultiplier)
        {
            Name = name;
            IconMethod = iconMethod;
            ScaleMultiplier = scaleMultiplier;
        }

        public int GetIconID(uint jobID)
        {
            // SUBTRACT FROM JOBID HERE
            var job = (Job)jobID - 1;
            if (IconMethod != null)
            {
                if (plugin == null)
                    throw new Exception("IconSet was not initialized");
                return IconMethod(job);
            }

            if (Icons != null)
                return Icons[(int)job];

            throw new Exception($"IconSet {Name} has no way of getting an Icon ID");
        }
    }
}
