using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PartOfYou.Runtime.Logic.System;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.Actions
{
    public class CopyAllStatistics : GameAction
    {
        [SerializeField] private GameObject toast;

        private void Awake()
        {
            toast.SetActive(false);
        }

        public override void Execute()
        {
            var levelIds = GameManager.Instance.levelLoad.GetAllLevelList();
            var resultString = string.Empty;
            foreach (var levelId in levelIds)
            {
                var levelStatistics = GameManager.Instance.SaveLoad.GetLevelPlayInfo(levelId);
                var statisticsString =
                    $"{levelStatistics.levelId} / Clears: {levelStatistics.clearCount}, Actions: {levelStatistics.actionCount}, Time: {levelStatistics.playTime.ToString()}\n";
                resultString += statisticsString;
            }

            GUIUtility.systemCopyBuffer = resultString;
            toast.SetActive(true);
            ToastClose().Forget();
        }

        private CancellationTokenSource _toastToken;
        private async UniTask ToastClose()
        {
            _toastToken?.Cancel();
            var tokenSource = new CancellationTokenSource();
            _toastToken = tokenSource;
            await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: tokenSource.Token);
            if (tokenSource.IsCancellationRequested)
            {
                return;
            }

            toast.SetActive(false);
        }

        private void OnDestroy()
        {
            _toastToken?.Cancel();
        }
    }
}