using System;
using System.Collections.Generic;
using System.Linq;
using Eflatun.SceneReference;
using UnityEngine;
using UnityEngine.Serialization;

namespace PartOfYou.Runtime.Logic.System
{
    [CreateAssetMenu(fileName = "LevelInfo", menuName = "ScriptableObjects/LevelInfoScriptableObject", order = 1)]
    public class LevelInfoScriptableObject : ScriptableObject
    {
        [Serializable]
        public class LevelInfo
        {
            [SerializeField] public int levelId;
            [SerializeField] public SceneReference scene;
        }

        [Serializable]
        public class ChapterInfo
        {
            [SerializeField] public int chapterId;
            [SerializeField] public string chapterName;
            [SerializeField] public List<LevelInfo> levelInfoList;
        }

        [SerializeField] public List<ChapterInfo> chapterInfoList;
    }
}