using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using PartOfYou.Runtime.Logic.Object;
using PartOfYou.Runtime.Utils;
using UniRx;
using Unity.Collections;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.Level
{
        public class LevelManager : SceneAnchor<LevelManager>
    {
        [SerializeField] private float moveDuration = 0.1f;

        private readonly Subject<TurnInfo> _turnStream = new();
        private readonly Stack<MoveGroup> _moveStacks = new();
        private readonly Stack<MoveGroup> _undoStacks = new();

        public IObservable<TurnInfo> TurnObservable => _turnStream.AsObservable();
        public Body playerBody;
        
        [ReadOnly]
        private int _turnNumber;

        public override void Awake()
        {
            base.Awake();
            StartLevel().Forget();
        }

        private async UniTask StartLevel()
        {
            //Todo: Load Level
            
            await LevelActionLoop();
            
            //Todo: Unload Level, Save Clear Info
        }
        
        private async UniTask LevelActionLoop()
        {
            var inputObservable = GetComponent<LevelInput>().InputAsObservable();

            var waitForInput = inputObservable.ToUniTask(true);

            //Todo: Clear Condition
            while (true)
            {
                var input = await waitForInput;

                // turn이 끝날 때 까지 기다렸다가, input이 있었다면 마지막 input을 실행, 아니라면 새 input이 올 때 까지 기다림.
                waitForInput = inputObservable
                    .CombineLatest(_turnStream, (nextInput, info) => nextInput)
                    .ToUniTask(true);
                
                switch (input)
                {
                    case Move move:
                        await Move(playerBody, move.Direction);
                        break;
                    case Restart _:
                        Restart();
                        break;
                    case Undo _:
                        await Undo();
                        break;
                    case Redo _:
                        await Redo();
                        break;
                }
            }
        }

        private void Restart()
        {
            _turnNumber = 0;
            //Todo: Reload Level
        }

        private async UniTask Move(Body body, Direction direction)
        {
            _undoStacks.Clear();
            var moveGroup = MoveGroup.GetGroup(body, direction);

            if (!moveGroup.Movable)
            {
                _turnStream.OnNext(new TurnInfo(Turn.None, _turnNumber));
                return;
            }
            
            _moveStacks.Push(moveGroup);

            await MoveAsync(moveGroup, InLevelTypeConverter.DirectionToVector2(direction), moveDuration);
            
            _turnStream.OnNext(new TurnInfo(Turn.Do, _turnNumber));
            _turnNumber++;
        }

        private async UniTask Undo()
        {
            if (_moveStacks.Count <= 0)
            {
                _turnStream.OnNext(new TurnInfo(Turn.None, _turnNumber));
                return;
            }
            
            var prevMoveGroup = _moveStacks.Pop();
            _undoStacks.Push(prevMoveGroup);
            
            var direction = prevMoveGroup.MoveDirection;
            await MoveAsync(prevMoveGroup, -InLevelTypeConverter.DirectionToVector2(direction), moveDuration);
            
            _turnStream.OnNext(new TurnInfo(Turn.Undo, _turnNumber));
            _turnNumber--;
        }

        private async UniTask Redo()
        {
            if (_undoStacks.Count <= 0)
            {
                _turnStream.OnNext(new TurnInfo(Turn.None, _turnNumber));
                return;
            }
            
            var redoMoveGroup = _undoStacks.Pop();
            _moveStacks.Push(redoMoveGroup);
            
            var direction = redoMoveGroup.MoveDirection;
            await MoveAsync(redoMoveGroup, InLevelTypeConverter.DirectionToVector2(direction), moveDuration);
            
            _turnStream.OnNext(new TurnInfo(Turn.Redo, _turnNumber));
            _turnNumber++;
        }

        private static async UniTask MoveAsync(MoveGroup moveGroup, Vector2 dirVector2, float duration)
        {
            await UniTask.WhenAll(
                moveGroup.GetTransforms.Select(
                    t => t
                        .DOTranslate(dirVector2, duration)
                        .SetEase(Ease.InOutSine)
                        .AsyncWaitForCompletion()
                        .AsUniTask()));
        }
        
        private void OnDestroy()
        {
            _turnStream.OnNext(new TurnInfo(Turn.None, _turnNumber));
            _turnStream.OnCompleted();
            _turnStream.Dispose();
        }
    }
}