using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using PartOfYou.Runtime.Logic.Object;
using PartOfYou.Runtime.Logic.Properties;
using PartOfYou.Runtime.Utils;
using UniRx;
using Unity.Collections;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.Level
{
        public class LevelManager : SceneAnchor<LevelManager>
    {
        [SerializeField] private float moveDuration = 0.1f;
        [SerializeField] private List<InputType> implicitUnlockedInputTypes = new()
        {
            InputType.Undo,
            InputType.Redo,
            InputType.Restart,
        };
        [SerializeField] private ColorTag defaultPlayerColorTag;

        private readonly Subject<TurnInfo> _turnStream = new();
        private readonly Stack<MoveGroup> _moveStacks = new();
        private readonly Stack<MoveGroup> _undoStacks = new();
        private readonly Dictionary<ColorTag, List<Body>> _registeredBodies = new();

        public IObservable<TurnInfo> TurnObservable => _turnStream.AsObservable();

        [ReadOnly]
        private int _turnNumber;

        public override void Awake()
        {
            base.Awake();
            StartLevel().Forget();
        }

        public void RegisterBody(Body body)
        {
            var colorTag = body is IHaveColor haveColor ? haveColor.ColorTag : ColorTag.None;
            if (!_registeredBodies.ContainsKey(colorTag))
            {
                _registeredBodies.Add(colorTag, new List<Body>());
            }
            
            _registeredBodies[colorTag].Add(body);
        }

        private async UniTask StartLevel()
        {
            await LevelActionLoop();
            
            //Todo: Save Clear Info, Move to Next Level (or Exit)
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
                    .CombineLatest(_turnStream, (nextInput, _) => nextInput)
                    .ToUniTask(true);

                var targetBody = GetTargetBody(input);

                if (implicitUnlockedInputTypes.Contains(input) || targetBody.Count <= 0)
                {
                    return;
                }

                switch (input)
                {
                    case InputType.Up:
                        await MoveAsync(targetBody, Direction.Up);
                        break;
                    case InputType.Down:
                        await MoveAsync(targetBody, Direction.Down);
                        break;
                    case InputType.Left:
                        await MoveAsync(targetBody, Direction.Left);
                        break;
                    case InputType.Right:
                        await MoveAsync(targetBody, Direction.Right);
                        break;
                    case InputType.Restart:
                        await RestartAsync();
                        break;
                    case InputType.Undo:
                        await UndoAsync();
                        break;
                    case InputType.Redo:
                        await RedoAsync();
                        break;
                }
            }
        }

        private List<Body> GetTargetBody(InputType inputType)
        {
            var targetBodies = new List<Body>();
            var colorSearchQueue = new Queue<ColorTag>();
            var colorList = new List<ColorTag>();
            colorSearchQueue.Enqueue(defaultPlayerColorTag);

            while (colorSearchQueue.Count > 0)
            {
                AddColor(colorSearchQueue.Dequeue());
            }

            foreach (var colorTag in colorList)
            {
                var coloredBodies = _registeredBodies[colorTag];
                var colorLinkedBodies = new List<Body>();
                foreach (var coloredBody in coloredBodies)
                {
                    colorLinkedBodies.Add(coloredBody);
                    colorLinkedBodies.AddRange(coloredBody.linkedBodies);
                }

                if (colorLinkedBodies.Any(x => x is IUnlockInput unlockInput && unlockInput.UnlockInput == inputType))
                {
                    // 연결된 body는 필요할 경우 다시 계산.
                    targetBodies.AddRange(coloredBodies);
                }
            }

            return targetBodies;

            void AddColor(ColorTag colorTag)
            {
                var coloredBodies = _registeredBodies[colorTag];
                foreach (var coloredBody in coloredBodies)
                {
                    colorList.Add(colorTag);
                    
                    var linkedBodies = coloredBody.linkedBodies;
                    foreach (var connectToColor in linkedBodies.OfType<IConnectToColor>())
                    {
                        colorSearchQueue.Enqueue(connectToColor.ConnectToColorTag);
                    }
                }
            }
        }

        private async UniTask RestartAsync()
        {
            _turnNumber = 0;
            //Todo: Reload Level
        }

        private async UniTask MoveAsync(IEnumerable<Body> bodies, Direction direction)
        {
            _undoStacks.Clear();

            var targetMoveGroup = new MoveGroup(direction);
            
            foreach (var body in bodies)
            {
                var moveGroup = MoveGroup.GetGroup(body, direction);

                if (!moveGroup.Movable)
                {
                    _turnStream.OnNext(new TurnInfo(Turn.None, _turnNumber));
                    continue;
                }
                
                targetMoveGroup.MergeGroup(moveGroup);
            }

            _moveStacks.Push(targetMoveGroup);

            await MoveAsync(targetMoveGroup, InLevelTypeConverter.DirectionToVector2(direction), moveDuration);
            
            _turnStream.OnNext(new TurnInfo(Turn.Do, _turnNumber));
            _turnNumber++;
        }

        private async UniTask UndoAsync()
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

        private async UniTask RedoAsync()
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