namespace CyberExtraction.Core
{
    public static class CharacterProgressionRules
    {
        public const int MaxLevel=20;
        public const long MaxExperience=10450;
        // Twice the lifetime cap already fills level 20 even under the 50% loss rule.
        public const long MaxPendingExperience=MaxExperience*2;
        public static int NextLevelCost(int level) => level>=MaxLevel ? 0 : 100+50*(level-1);
        public static long LevelStart(int level)
        {
            long xp=0;
            for(int i=1;i<level && i<MaxLevel;i++) xp+=NextLevelCost(i);
            return xp;
        }
        public static int LevelAt(long xp)
        {
            int level=1;
            while(level<MaxLevel && xp>=LevelStart(level+1)) level++;
            return level;
        }
        public static long ConfirmedExperience(long pending,bool extracted)
        {
            pending=System.Math.Max(0,System.Math.Min(MaxPendingExperience,pending));
            return extracted ? pending*6/5 : pending/2;
        }
    }
}
