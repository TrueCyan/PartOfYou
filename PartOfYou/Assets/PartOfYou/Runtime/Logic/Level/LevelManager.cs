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
using UnityEngine.SceneManagement;

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
        [SerializeField] private ColorTag defaultPlayerColorTag  = ColorTag.White;
        [SerializeField] private string nextLevel;

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
        }
        
        private async UniTask LevelActionLoop()
        {
            var inputObservable = GetComponent<LevelInput>().InputAsObservable();

            var waitForInput = inputObservable.ToUniTask(true);

            while (true)
            {
                var input = await waitForInput;

                var availableYou = GetAvailableYou().ToList();

                var availableInput = GetAvailableInput(availableYou);

                var currentInputDict = availableInput
                    .Where(x => x.Value.ContainsKey(input))
                    .ToDictionary(x => x.Key, x => x.Value[input]);

                if (!implicitUnlockedInputTypes.Contains(input)
                    && currentInputDict.All(x => x.Value <= 0))
                {
                    waitForInput = inputObservable.ToUniTask(true);
                    continue;
                }
                
                // turn이 끝날 때 까지 기다렸다가, input이 있었다면 마지막 input을 실행, 아니라면 새 input이 올 때 까지 기다림.
                waitForInput = inputObservable
                    .CombineLatest(_turnStream, (nextInput, _) => nextInput)
                    .ToUniTask(true);

                var turnAction = input switch
                {
                    InputType.Up => await MoveAsync(availableYou, currentInputDict, Direction.Up),
                    InputType.Down => await MoveAsync(availableYou, currentInputDict, Direction.Down),
                    InputType.Left => await MoveAsync(availableYou, currentInputDict, Direction.Left),
                    InputType.Right => await MoveAsync(availableYou, currentInputDict, Direction.Right),
                    InputType.Restart => RestartRequest(),
                    InputType.Undo => await UndoAsync(),
                    InputType.Redo => await RedoAsync(),
                    _ => throw new ArgumentOutOfRangeException()
                };
                
                if (turnAction == TurnAction.Restart)
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    waitForInput.DisposeUniTask();
                    return;
                }

                var cleared = availableYou
                    .Any(LevelQuery.IsBodyOnGoal);

                if (cleared)
                {
                    SceneManager.LoadScene(nextLevel);
                    waitForInput.DisposeUniTask();
                    return;
                }
                
                _turnStream.OnNext(new TurnInfo(turnAction, _turnNumber));
                switch (turnAction)
                {
                    case TurnAction.None:
                        break;
                    case TurnAction.Do:
                    case TurnAction.Redo:
                        _turnNumber++;
                        break;
                    case TurnAction.Undo:
                        _turnNumber--;
                        break;
                    case TurnAction.Restart:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private IEnumerable<Body> GetAvailableYou()
        {
            var allLinkedBody = new List<Body>();
            var colorSearchQueue = new Queue<Body>();

            var defaultBodies = _registeredBodies[defaultPlayerColorTag];
            foreach (var body in defaultBodies)
            {
                colorSearchQueue.Enqueue(body);
            }

            while (colorSearchQueue.Count > 0)
            {
                SearchColorLinkedBodies(colorSearchQueue.Dequeue());
            }

            return allLinkedBody.Where(x => x is You);

            void SearchColorLinkedBodies(Body body)
            {
                allLinkedBody.Add(body);
                
                if (body is IConnectToColor connectToColor)
                {
                    var coloredBodies = _registeredBodies[connectToColor.ConnectToColorTag];
                    foreach (var coloredBody in coloredBodies)
                    {
                        if (!allLinkedBody.Contains(coloredBody))
                        {
                            colorSearchQueue.Enqueue(coloredBody);
                        }
                    }
                }

                if (body is You or ICanAttachToYou)
                {
                    var nearBodies = LevelQuery.GetNearBody(body);
                    foreach (var nearBody in nearBodies)
                    {
                        if (!allLinkedBody.Contains(nearBody))
                        {
                            colorSearchQueue.Enqueue(nearBody);
                        }
                    }
                }
                
            }
        }

        private Dictionary<ColorTag, Dictionary<InputType, int>> GetAvailableInput(IEnumerable<Body> bodies)
        {
            var availableInputDict = new Dictionary<ColorTag, Dictionary<InputType, int>>();

            foreach (var checkingBody in bodies)
            {
                if (checkingBody is not IHaveColor haveColor)
                {
                    continue;
                }

                var currentColor = haveColor.ColorTag;
                if (!availableInputDict.ContainsKey(currentColor))
                {
                    availableInputDict.Add(currentColor, new Dictionary<InputType, int>());
                }

                var attachCheckedList = new List<Body>();
                var attachCheckStack = new Stack<Body>();
                attachCheckStack.Push(checkingBody);

                while (attachCheckStack.Count > 0)
                {
                    var body = attachCheckStack.Pop();
                    foreach (var nearBody in LevelQuery.GetNearBody(body))
                    {
                        if (nearBody is not ICanAttachToYou
                            || attachCheckedList.Contains(nearBody)
                            || attachCheckStack.Contains(nearBody))
                        {
                            continue;
                        }
                        
                        attachCheckStack.Push(nearBody);

                        if (nearBody is not IUnlockInput unlockInput)
                        {
                            continue;
                        }

                        if (!availableInputDict[currentColor].ContainsKey(unlockInput.UnlockInput))
                        {
                            availableInputDict[currentColor].Add(unlockInput.UnlockInput, 0);
                        }

                        availableInputDict[currentColor][unlockInput.UnlockInput]++;
                    }

                    attachCheckedList.Add(body);
                }
            }

            return availableInputDict;
        }

        private TurnAction RestartRequest()
        {
            return TurnAction.Restart;
        }

        private async UniTask<TurnAction> MoveAsync(List<Body> bodies, Dictionary<ColorTag, int> colorMoveCount, Direction direction)
        {
            _undoStacks.Clear();

            var moveCheckStack = new Stack<Body>();
            foreach (var body in bodies)
            {
                moveCheckStack.Push(body);
            }

            var targetMoveGroup = new MoveGroup(direction);

            while (moveCheckStack.Count > 0)
            {
                var body = moveCheckStack.Pop();
                var moveGroup = MoveGroup.GetGroup(body, direction);
                if (!moveGroup.Movable)
                {
                    continue;
                }
                
                targetMoveGroup.MergeGroup(moveGroup);
                var nearBodies = moveGroup
                    .GroupedBodies
                    .SelectMany(LevelQuery.GetNearBody)
                    .Distinct()
                    .Where(x => x is ICanAttachToYou && !targetMoveGroup.Contains(x));
                foreach (var nearBody in nearBodies)
                {
                    moveCheckStack.Push(nearBody);
                }
            }

            _moveStacks.Push(targetMoveGroup);

            await MoveAsync(targetMoveGroup, InLevelTypeConverter.DirectionToVector2(direction), moveDuration);

            var newDict = colorMoveCount
                .Where(x => x.Value > 1)
                .ToDictionary(x => x.Key, x => x.Value - 1);

            if (newDict.Count <= 0)
            {
                return TurnAction.Do;
            }
            
            var newBody = bodies
                .Where(x =>
                    newDict.ContainsKey(x is IHaveColor color ? color.ColorTag : ColorTag.None))
                .ToList();
            return await MoveAsync(newBody, newDict, direction);
        }

        private async UniTask<TurnAction> UndoAsync()
        {
            if (_moveStacks.Count <= 0)
            {
                return TurnAction.None;
            }
            
            var prevMoveGroup = _moveStacks.Pop();
            _undoStacks.Push(prevMoveGroup);
            
            var direction = prevMoveGroup.MoveDirection;
            await MoveAsync(prevMoveGroup, -InLevelTypeConverter.DirectionToVector2(direction), moveDuration);

            return TurnAction.Undo;
        }

        private async UniTask<TurnAction> RedoAsync()
        {
            if (_undoStacks.Count <= 0)
            {
                return TurnAction.None;
            }
            
            var redoMoveGroup = _undoStacks.Pop();
            _moveStacks.Push(redoMoveGroup);
            
            var direction = redoMoveGroup.MoveDirection;
            await MoveAsync(redoMoveGroup, InLevelTypeConverter.DirectionToVector2(direction), moveDuration);

            return TurnAction.Redo;
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
            _turnStream.OnNext(new TurnInfo(TurnAction.None, _turnNumber));
            _turnStream.OnCompleted();
            _turnStream.Dispose();
        }
    }
}