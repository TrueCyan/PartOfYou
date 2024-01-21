using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PartOfYou.Runtime.Logic.System
{
    public class LevelSelector : MonoBehaviour
    {
        [SerializeField] private GameObject clearMark;
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI text;

        private LevelId _levelId;

        public void Initialize(LevelId levelId)
        {
            var isCleared = GameManager.Instance.SaveLoad.GetLevelPlayInfo(levelId).isCleared;
            clearMark.SetActive(isCleared);
            button.onClick.AddListener(() => GameManager.Instance.transition.OpenLevel(levelId).Forget());
            text.text = $"{(int)levelId % 1000}";
        }
    }
}