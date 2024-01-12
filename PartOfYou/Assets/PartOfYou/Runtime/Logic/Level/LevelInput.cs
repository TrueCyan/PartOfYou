using System;
using UniRx;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.Level
{
    public class LevelInput : MonoBehaviour
    {
        private readonly Subject<Input> _inputStream = new();

        public IObservable<Input> InputAsObservable() => _inputStream.AsObservable();

        private void InputDirection(string direction)
        {
            _inputStream.OnNext(new Move(direction switch
            {
                "Up" => Direction.Up,
                "Down" => Direction.Down,
                "Left" => Direction.Left,
                "Right" => Direction.Right,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction,
                    "[Controller.cs] 유효한 방향 이름을 입력해 주세요.")
            }));
        }

        private void InputRestart()
        {
            _inputStream.OnNext(new Restart());
        }

        private void InputUndo()
        {
            _inputStream.OnNext(new Undo());
        }

        private void InputRedo()
        {
            _inputStream.OnNext(new Redo());
        }

        public void OnDestroy()
        {
            _inputStream.OnCompleted();
            _inputStream.Dispose();
        }

        public void OnLeft() => InputDirection("Left");
        public void OnRight() => InputDirection("Right");
        public void OnUp() => InputDirection("Up");
        public void OnDown() => InputDirection("Down");
        public void OnRestart() => InputRestart();
        public void OnUndo() => InputUndo();
        public void OnRedo() => InputRedo();
    }
}