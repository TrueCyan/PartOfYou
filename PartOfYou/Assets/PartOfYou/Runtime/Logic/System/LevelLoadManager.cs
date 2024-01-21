using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.System
{
    [Serializable]
    public class LevelLoadManager
    {
        [SerializeField] private LevelInfoScriptableObject _levelInfoScriptableObject;
        
        private Dictionary<LevelId, string> _levelSceneDictionary;

        private Dictionary<ChapterId, List<LevelId>>
            _chapterLevelDictionary;
        
        private Dictionary<ChapterId, string>
            _chapterNameDictionary;

        private List<ChapterId> _chapterList;

        public void Initialize()
        {
            _levelSceneDictionary = _levelInfoScriptableObject.chapterInfoList
                .SelectMany(x => x.levelInfoList)
                .ToDictionary(x => (LevelId)x.levelId, x => x.scene.Name);

            _chapterLevelDictionary = _levelInfoScriptableObject.chapterInfoList
                .ToDictionary(
                    x => (ChapterId)x.chapterId,
                    x => x.levelInfoList.Select(info => (LevelId)info.levelId).ToList());
            
            _chapterNameDictionary = _levelInfoScriptableObject.chapterInfoList.ToDictionary(x => (ChapterId)x.chapterId, x => x.chapterName);
            _chapterList = _levelInfoScriptableObject.chapterInfoList.Select(x => (ChapterId)x.chapterId)
                .ToList();
        }

        public string GetLevelSceneName(LevelId levelId)
        {
            if (_levelSceneDictionary == null || !_levelSceneDictionary.ContainsKey(levelId))
            {
                throw new ArgumentException("레벨 정보를 불러올 수 없습니다.");
            }

            return _levelSceneDictionary[levelId];
        }

        public List<LevelId> GetChapterLevelList(ChapterId chapterId)
        {
            if (_chapterLevelDictionary == null || !_chapterLevelDictionary.ContainsKey(chapterId))
            {
                throw new ArgumentException("챕터 정보를 불러올 수 없습니다.");
            }

            return _chapterLevelDictionary[chapterId];
        }
        
        public string GetChapterName(ChapterId chapterId)
        {
            if (_chapterNameDictionary == null || !_chapterNameDictionary.ContainsKey(chapterId))
            {
                throw new ArgumentException("챕터 정보를 불러올 수 없습니다.");
            }

            return _chapterNameDictionary[chapterId];
        }

        public List<ChapterId> GetChapterList()
        {
            if (_chapterList == null)
            {
                throw new ArgumentException("챕터 목록을 불러올 수 없습니다.");
            }

            return _chapterList;
        }

        public List<LevelId> GetAllLevelList()
        {
            return _levelSceneDictionary.Keys.ToList();
        }
    }
}