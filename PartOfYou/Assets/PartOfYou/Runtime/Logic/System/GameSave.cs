using System;
using System.Collections.Generic;
using System.Linq;
using PartOfYou.Runtime.Logic.Level;
using UnityEngine;
using UnityEngine.Serialization;

namespace PartOfYou.Runtime.Logic.System
{
    [Serializable]
    public class GameSave
    {
        [SerializeField] public List<LevelPlayInfo> levelPlayInfoList = new();

        private Dictionary<LevelId, LevelPlayInfo> _levelPlayInfoDict = new();

        public void AddPlayInfo(LevelPlayInfo levelPlayInfo)
        {
            levelPlayInfoList.Add(levelPlayInfo);
            _levelPlayInfoDict[(LevelId)levelPlayInfo.levelId] = levelPlayInfo;
        }

        public LevelPlayInfo GetPlayInfo(LevelId levelId)
        {
            return _levelPlayInfoDict.TryGetValue(levelId, out var playInfo) ? playInfo : new LevelPlayInfo(levelId, false, new LevelStatistics(TimeSpan.Zero, 0));
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
            return gameSave;
        }
    }
    
    [Serializable]
    public class LevelPlayInfo
    {
        [SerializeField] public int levelId;
        [SerializeField] public bool isCleared;
        [SerializeField] public PlayTime playTime;
        [SerializeField] public int actionCount;

        public LevelPlayInfo(LevelId levelId, bool isCleared, LevelStatistics levelStatistics)
        {
            this.levelId = (int)levelId;
            this.isCleared = isCleared;
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
            return new PlayTime((int)timeSpan.TotalHours, timeSpan.Minutes, timeSpan.Seconds + 1); // millisecond 올림 처리
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
    }
}