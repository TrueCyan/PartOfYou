using System;
using UniRx;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.Level
{
    public class LevelInput : MonoBehaviour
    {
        private readonly Subject<InputType> _inputStream = new();

        public IObservable<InputType> InputAsObservable() => _inputStream.AsObservable();

        public void OnDestroy()
        {
            _inputStream.OnCompleted();
            _inputStream.Dispose();
        }

        public void OnLeft() => _inputStream.OnNext(InputType.Left);
        public void OnRight() => _inputStream.OnNext(InputType.Right);
        public void OnUp() => _inputStream.OnNext(InputType.Up);
        public void OnDown() => _inputStream.OnNext(InputType.Down);
        public void OnRestart() => _inputStream.OnNext(InputType.Restart);
        public void OnUndo() => _inputStream.OnNext(InputType.Undo);
        public void OnRedo() => _inputStream.OnNext(InputType.Redo);
    }
}