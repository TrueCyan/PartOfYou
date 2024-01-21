using System;
using Cysharp.Threading.Tasks;
using PartOfYou.Runtime.Logic.Level;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace PartOfYou.Runtime.Logic.System
{
    [Serializable]
    public class TransitionManager
    {
        [SerializeField] private PlayableDirector playableDirector;
        [SerializeField] private PlayableAsset fadeIn;
        [SerializeField] private PlayableAsset fadeOut;
        [SerializeField] private TextMeshProUGUI transitionText;

        public UniTask ToTitle()
        {
            return SceneMove("Title");
        }

        public UniTask ToStageSelect()
        {
            return SceneMove("Stage select");
        }

        public async UniTask OpenLevel(LevelId levelId)
        {
            var sceneName = GameManager.Instance.levelLoad.GetLevelSceneName(levelId);
            playableDirector.Play(fadeIn);
            await UniTask.Delay(TimeSpan.FromSeconds(fadeIn.duration));
            transitionText.text = sceneName;
            await UniTask.Delay(TimeSpan.FromSeconds(1));
            transitionText.text = string.Empty;
            SceneManager.LoadScene(sceneName);
            await UniTask.DelayFrame(1);
            LevelManager.Instance.SetLevelId(levelId);
            playableDirector.Play(fadeOut);
            await UniTask.Delay(TimeSpan.FromSeconds(fadeOut.duration));
        }

        public async UniTask SceneMove(string targetScene)
        {
            transitionText.text = string.Empty;
            playableDirector.Play(fadeIn);
            await UniTask.Delay(TimeSpan.FromSeconds(fadeIn.duration));
            SceneManager.LoadScene(targetScene);
            playableDirector.Play(fadeOut);
            await UniTask.Delay(TimeSpan.FromSeconds(fadeOut.duration));
        }
    }
}