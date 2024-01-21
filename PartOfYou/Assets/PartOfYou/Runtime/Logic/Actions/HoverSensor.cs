using System;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace PartOfYou.Runtime.Logic.Actions
{
    public class HoverSensor : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private GameAction gameAction;

        private IDisposable _disposable;
        private void Start()
        {
            button.OnPointerEnterAsObservable().AsObservable().Subscribe(x => OnHover());
        }

        private void OnDestroy()
        {
            _disposable?.Dispose();
        }

        private void OnHover()
        {
            gameAction.Execute();
        }
    }
}