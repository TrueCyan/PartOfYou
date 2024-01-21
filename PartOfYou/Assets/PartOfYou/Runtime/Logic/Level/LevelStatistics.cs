using System;

namespace PartOfYou.Runtime.Logic.Level
{
    public class LevelStatistics
    {
        public TimeSpan PlayTime { get; }
        public int ActionCount { get; }

        public LevelStatistics(TimeSpan playTime, int actionCount)
        {
            PlayTime = playTime;
            ActionCount = actionCount;
        }
    }
}