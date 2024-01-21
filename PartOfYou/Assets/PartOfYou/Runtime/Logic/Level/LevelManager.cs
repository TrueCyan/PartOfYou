using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using PartOfYou.Runtime.Logic.Object;
using PartOfYou.Runtime.Logic.Properties;
using PartOfYou.Runtime.Logic.System;
using PartOfYou.Runtime.Utils;
using UniRx;
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
        [SerializeField] private ColorTag defaultPlayerColorTag  = ColorTag.White;
        [SerializeField] private bool startOnAwake;

        private readonly Subject<Unit> _turnStream = new();
        private readonly Stack<TurnCommand> _commandStacks = new();
        private readonly Stack<TurnCommand> _undoStacks = new();
        private readonly Dictionary<ColorTag, List<Body>> _registeredBodies = new();

        private List<(Body Body, Vector3 Position)> _initialSnapshot;

        private readonly List<Sensor> _sensors = new();
        private int _actionCount;
        private LevelId _levelId;
        private CancellationTokenSource _levelCancellationTokenSource;

        public override void Awake()
        {
            base.Awake();
            if (startOnAwake)
            {
                StartLevel().Forget();
            }
        }

        public void SetLevelId(LevelId levelId)
        {
            _levelId = levelId;
            if (!startOnAwake)
            {
                StartLevel().Forget();
            }
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

        public void RegisterSensor(Sensor sensor)
        {
            _sensors.Add(sensor);
        }

        private List<(Body, Vector3)> GetSnapshot()
        {
            return _registeredBodies
                .SelectMany(x => x.Value)
                .Select(x => (x, x.transform.position))
                .ToList();
        }

        private bool _quitGame;
        private async UniTask StartLevel()
        {
            _levelCancellationTokenSource?.Cancel();
            _levelCancellationTokenSource = new CancellationTokenSource();
            _initialSnapshot = GetSnapshot();

            LevelStatistics prevData;
            var prevClearCount = 0;
            if (_levelId != LevelId.None)
            {
                var playInfo = GameManager.Instance.SaveLoad.GetLevelPlayInfo(_levelId);
                prevData = new LevelStatistics(playInfo.playTime.ToTimeSpan(), playInfo.actionCount);
                prevClearCount = playInfo.clearCount;
            }
            else
            {
                prevData = new LevelStatistics(TimeSpan.Zero, 0);
            }

            var startTime = DateTime.Now;
            var cleared = await LevelActionLoop();
            var endTime = DateTime.Now;
            var elapsedTime = endTime - startTime;

            var levelStatistics = new LevelStatistics(prevData.PlayTime + elapsedTime, prevData.ActionCount + _actionCount);

            if (_levelId != LevelId.None)
            {
                GameManager.Instance.SaveLoad.ExitLevel(_levelId, levelStatistics, prevClearCount + (cleared ? 1 : 0));
                if (!_quitGame)
                {
                    await GameManager.Instance.transition.ToStageSelect();
                }
            }
        }

        private async UniTask<bool> LevelActionLoop()
        {
            var inputObservable = GetComponent<LevelInput>().InputAsObservable();

            var waitForInput = inputObservable.ToUniTask(true);

            while (true)
            {
                var (isCanceled, input) = await waitForInput
                    .AttachExternalCancellation(_levelCancellationTokenSource.Token)
                    .SuppressCancellationThrow();
                
                if (isCanceled)
                {
                    return false;
                }

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

                switch (input)
                {
                    case InputType.Up:
                        await MoveAsync(availableYou, currentInputDict, Direction.Up);
                        break;
                    case InputType.Down:
                        await MoveAsync(availableYou, currentInputDict, Direction.Down);
                        break;
                    case InputType.Left:
                        await MoveAsync(availableYou, currentInputDict, Direction.Left);
                        break;
                    case InputType.Right:
                        await MoveAsync(availableYou, currentInputDict, Direction.Right);
                        break;
                    case InputType.Restart:
                        RestartCommand();
                        break;
                    case InputType.Undo:
                        await UndoAsync();
                        break;
                    case InputType.Redo:
                        await RedoAsync();
                        break;
                    case InputType.Enter:
                        await ActivateAsync();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var youOnGoal = availableYou
                    .OfType<You>()
                    .Where(LevelQuery.IsBodyOnGoal)
                    .ToList();

                if (youOnGoal.Any())
                {
                    waitForInput.DisposeUniTask();
                    await UniTask.WhenAll(youOnGoal.Select(x => x.Ascend()));
                    return true;
                }
                
                _turnStream.OnNext(Unit.Default);
            }
        }

        public void ExitLevel()
        {
            _levelCancellationTokenSource?.Cancel();
        }

        private void OnApplicationQuit()
        {
            _quitGame = true;
            _levelCancellationTokenSource?.Cancel();
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

            var attachCheckedForColorDict = new Dictionary<ColorTag, List<Body>>();
            foreach (var checkingBody in bodies)
            {
                if (checkingBody is not IHaveColor haveColor)
                {
                    continue;
                }

                var currentColor = haveColor.ColorTag;
                if (!attachCheckedForColorDict.ContainsKey(currentColor))
                {
                    attachCheckedForColorDict.Add(currentColor, new List<Body>());
                }

                if (!availableInputDict.ContainsKey(currentColor))
                {
                    availableInputDict.Add(currentColor, new Dictionary<InputType, int>());
                }

                var attachCheckStack = new Stack<Body>();
                attachCheckStack.Push(checkingBody);

                while (attachCheckStack.Count > 0)
                {
                    var body = attachCheckStack.Pop();
                    foreach (var nearBody in LevelQuery.GetNearBody(body))
                    {
                        if (nearBody is not ICanAttachToYou
                            || attachCheckedForColorDict[currentColor].Contains(nearBody)
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

                    attachCheckedForColorDict[currentColor].Add(body);
                }
            }

            return availableInputDict;
        }

        private void RestartCommand()
        {
            _undoStacks.Clear();
            var command = new RestartCommand(GetSnapshot(), _initialSnapshot);
            ApplySnapshot(command.NewPos);
            _commandStacks.Push(command);
        }

        private async UniTask MoveAsync(List<Body> bodies, Dictionary<ColorTag, int> colorMoveCount, Direction direction)
        {
            var isValidMove = false;
            var moveCommand = new MoveCommand();
            while (true)
            {
                _undoStacks.Clear();

                var moveCheckStack = new Stack<Body>();
                foreach (var body in bodies)
                {
                    moveCheckStack.Push(body);
                }

                var moveInfo = new MoveInfo(direction);

                while (moveCheckStack.Count > 0)
                {
                    var body = moveCheckStack.Pop();
                    var targetMoveInfo = MoveInfo.GetCommand(body, direction);
                    if (!targetMoveInfo.Movable)
                    {
                        continue;
                    }

                    moveInfo.MergeTarget(targetMoveInfo);
                    var nearBodies = targetMoveInfo.GroupedBodies.SelectMany(LevelQuery.GetNearBody)
                        .Distinct()
                        .Where(x => x is ICanAttachToYou && !moveInfo.Contains(x));
                    foreach (var nearBody in nearBodies)
                    {
                        moveCheckStack.Push(nearBody);
                    }
                }

                moveCommand.AddInfo(moveInfo);

                await MoveAsync(moveInfo, InLevelTypeConverter.DirectionToVector2(direction), moveDuration);
                if (moveInfo.GroupedBodies.Count > 0)
                {
                    isValidMove = true;
                }

                var newDict = colorMoveCount.Where(x => x.Value > 1)
                    .ToDictionary(x => x.Key, x => x.Value - 1);

                if (newDict.Count <= 0)
                {
                    break;
                }

                var newBody = bodies.Where(x => newDict.ContainsKey(x is IHaveColor color ? color.ColorTag : ColorTag.None))
                    .ToList();
                bodies = newBody;
                colorMoveCount = newDict;
            }
            
            _commandStacks.Push(moveCommand);
            if (isValidMove)
            {
                _actionCount++;
            }
        }

        private async UniTask<TurnAction> UndoAsync()
        {
            if (_commandStacks.Count <= 0)
            {
                return TurnAction.None;
            }

            var command = _commandStacks.Pop();
            _undoStacks.Push(command);
            switch (command)
            {
                case MoveCommand moveCommand:
                    foreach (var moveInfo in moveCommand.MoveInfos.Reverse())
                    {
                        var direction = moveInfo.MoveDirection;
                        await MoveAsync(moveInfo, -InLevelTypeConverter.DirectionToVector2(direction), moveDuration);
                    }
                    
                    break;
                case RestartCommand restartCommand:
                    ApplySnapshot(restartCommand.PrevPos);
                    break;
            }
            

            return TurnAction.Undo;
        }

        private async UniTask<TurnAction> RedoAsync()
        {
            if (_undoStacks.Count <= 0)
            {
                return TurnAction.None;
            }

            var command = _undoStacks.Pop();
            _commandStacks.Push(command);
            switch (command)
            {
                case MoveCommand moveCommand:
                    foreach (var moveInfo in moveCommand.MoveInfos)
                    {
                        var direction = moveInfo.MoveDirection;
                        await MoveAsync(moveInfo, InLevelTypeConverter.DirectionToVector2(direction), moveDuration);
                    }

                    break;
                case RestartCommand restartCommand:
                    ApplySnapshot(restartCommand.NewPos);
                    break;
            }

            return TurnAction.Redo;
        }

        private static async UniTask MoveAsync(MoveInfo moveInfo, Vector2 dirVector2, float duration)
        {
            await UniTask.WhenAll(
                moveInfo.GetTransforms.Select(
                    t => t
                        .DOTranslate(dirVector2, duration)
                        .SetEase(Ease.InOutSine)
                        .AsyncWaitForCompletion()
                        .AsUniTask()));
        }

        private async UniTask ActivateAsync()
        {
            foreach (var sensor in _sensors)
            {
                sensor.TryActivate();
            }
            
            // Todo: 센서 활성화 동작 중 undo가 필요한 작업이 생길 경우 undo 구현.
        }

        private static void ApplySnapshot(List<(Body, Vector3)> snapshot)
        {
            foreach (var (body, position) in snapshot)
            {
                body.transform.position = position;
            }
        }
        
        private void OnDestroy()
        {
            _levelCancellationTokenSource.Cancel();
            _turnStream.OnCompleted();
            _turnStream.Dispose();
        }
    }
}