using TMPro;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.System
{
    public class SimpleChapterLevelList : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private RectTransform stageListRoot;
        [SerializeField] private LevelSelector levelSelectorPrefab;

        public void Initialize(ChapterId chapterId)
        {
            text.text = GameManager.Instance.levelLoad.GetChapterName(chapterId);
            var levelList = GameManager.Instance.levelLoad.GetChapterLevelList(chapterId);

            foreach (var levelId in levelList)
            {
                var selector = Instantiate(levelSelectorPrefab, stageListRoot);
                selector.gameObject.SetActive(true);
                selector.Initialize(levelId);
            }
        }
    }
}