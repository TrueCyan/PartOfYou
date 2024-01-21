using System;
using System.Collections.Generic;
using System.Linq;
using PartOfYou.Runtime.Logic.Level;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.System
{
    [Serializable]
    public class GameSave
    {
        private const int currentSaveVersion = 1;
        public int SaveVersion = currentSaveVersion;
        [SerializeField] public List<LevelPlayInfo> levelPlayInfoList = new();
        private Dictionary<LevelId, LevelPlayInfo> _levelPlayInfoDict = new();

        public void AddPlayInfo(LevelPlayInfo levelPlayInfo)
        {
            _levelPlayInfoDict[(LevelId)levelPlayInfo.levelId] = levelPlayInfo;
            levelPlayInfoList = _levelPlayInfoDict.Values.ToList();
        }

        public LevelPlayInfo GetPlayInfo(LevelId levelId)
        {
            return _levelPlayInfoDict.TryGetValue(levelId, out var playInfo) ? playInfo : new LevelPlayInfo(levelId, 0, new LevelStatistics(TimeSpan.Zero, 0));
        }

        private void Initialize()
        {
            _levelPlayInfoDict = levelPlayInfoList.ToDictionary(x => (LevelId)x.levelId);
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        public static GameSave FromJson(string json)
        {
            var gameSave = JsonUtility.FromJson<GameSave>(json);
            gameSave.Initialize();
            if (currentSaveVersion != gameSave.SaveVersion)
            {
                throw new Exception("Save is outdated.");
            }

            return gameSave;
        }
    }
    
    [Serializable]
    public class LevelPlayInfo
    {
        [SerializeField] public int levelId;
        [SerializeField] public int clearCount;
        [SerializeField] public PlayTime playTime;
        [SerializeField] public int actionCount;

        public LevelPlayInfo(LevelId levelId, int clearCount, LevelStatistics levelStatistics)
        {
            this.levelId = (int)levelId;
            this.clearCount = clearCount;
            playTime = PlayTime.FromTimeSpan(levelStatistics.PlayTime);
            actionCount = levelStatistics.ActionCount;
        }
    }

    [Serializable]
    public class PlayTime
    {
        [SerializeField] public int hour;
        [SerializeField] public int minute;
        [SerializeField] public int second;

        public static PlayTime FromTimeSpan(TimeSpan timeSpan)
        {
            var millisecondCeil = timeSpan.Milliseconds > 0 ? 1 : 0;
            
            return new PlayTime((int)timeSpan.TotalHours, timeSpan.Minutes, timeSpan.Seconds + millisecondCeil);
        }

        private PlayTime(int hour, int minute, int second)
        {
            this.hour = hour;
            this.minute = minute;
            this.second = second;
        }

        public TimeSpan ToTimeSpan()
        {
            return new TimeSpan(hour, minute, second);
        }

        public new string ToString()
        {
            return $"{hour}:{minute:00}:{second:00}";
        }
    }
}