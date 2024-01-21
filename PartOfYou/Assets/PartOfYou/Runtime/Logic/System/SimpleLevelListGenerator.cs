using UnityEngine;

namespace PartOfYou.Runtime.Logic.System
{
    public class SimpleLevelListGenerator : MonoBehaviour
    {
        [SerializeField] private RectTransform chapterListRoot;
        [SerializeField] private SimpleChapterLevelList chapterLevelListPrefab;

        private void Awake()
        {
            var chapterList = GameManager.Instance.levelLoad.GetChapterList();

            foreach (var chapterId in chapterList)
            {
                var chapterLevelList = Instantiate(chapterLevelListPrefab, chapterListRoot);
                chapterLevelList.gameObject.SetActive(true);
                chapterLevelList.Initialize(chapterId);
            }
        }
    }
}