using System;
using Cysharp.Threading.Tasks;
using PartOfYou.Runtime.Logic.Level;
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
            playableDirector.Play(fadeIn);
            await UniTask.Delay(TimeSpan.FromSeconds(fadeIn.duration));
            var sceneName = GameManager.Instance.levelLoad.GetLevelSceneName(levelId);
            SceneManager.LoadScene(sceneName);
            await UniTask.DelayFrame(1);
            LevelManager.Instance.SetLevelId(levelId);
            playableDirector.Play(fadeOut);
            await UniTask.Delay(TimeSpan.FromSeconds(fadeOut.duration));
        }

        public async UniTask SceneMove(string targetScene)
        {
            playableDirector.Play(fadeIn);
            await UniTask.Delay(TimeSpan.FromSeconds(fadeIn.duration));
            SceneManager.LoadScene(targetScene);
            playableDirector.Play(fadeOut);
            await UniTask.Delay(TimeSpan.FromSeconds(fadeOut.duration));
        }
    }
}