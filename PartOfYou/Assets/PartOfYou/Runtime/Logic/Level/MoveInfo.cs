using System.Collections.Generic;
using System.Linq;
using PartOfYou.Runtime.Logic.Object;
using PartOfYou.Runtime.Utils;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.Level
{
    public class MoveInfo
    {
        public readonly List<Body> GroupedBodies;
        
        public readonly Direction MoveDirection;

        public bool Movable = true;
        
        public IEnumerable<Transform> GetTransforms => GroupedBodies.Select(block => block.transform);

        public MoveInfo(Direction direction)
        {
            MoveDirection = direction;
            GroupedBodies = new List<Body>();
        }

        private void ExtendTarget(Body body)
        {
            if (!GroupedBodies.Contains(body))
            {
                GroupedBodies.Add(body);
            }
            else
            {
                Debug.LogError("[ActionManager.cs] !! Trying to add same Body to the MoveGroup !!");
            }
        }

        public void MergeTarget(MoveInfo moveInfo)
        {
            foreach (var body in moveInfo.GroupedBodies)
            {
                if (!GroupedBodies.Contains(body))
                {
                    GroupedBodies.Add(body);
                }
            }
        }

        public bool Contains(Body body)
        {
            return GroupedBodies.Contains(body);
        }
        
        public static MoveInfo GetCommand(Body body, Direction direction)
        {
            var moveGroup = new MoveInfo(direction);
            
            var dir = InLevelTypeConverter.DirectionToVector2(direction);

            var queue = new Queue<Body>();
            queue.Enqueue(body);

            // ReSharper disable once SuggestBaseTypeForParameter
            void AddFrontBodyToQueue(Body target)
            {
                var front = LevelQuery.GetFrontBody(target, dir);
                if (front.Exists() && !moveGroup.Contains(front))
                {
                    queue.Enqueue(front);
                }
            }
            
            while (queue.Count > 0)
            {
                var next = queue.Dequeue();
                
                if (moveGroup.Contains(next)) continue;

                if (!next.Movable || !LevelQuery.CheckFrontHasFloor(next, dir))
                {
                    moveGroup.Movable = false;
                    break;
                }
                
                var group = next.strongLinkedGroup;
                if (group != null)
                {
                    foreach (var member
                        in group.Members.Where(member => !moveGroup.Contains(member)))
                    {
                        moveGroup.ExtendTarget(member);

                        if (member.isActiveAndEnabled)
                        {
                            AddFrontBodyToQueue(member);
                        }
                    }
                }
                else
                {
                    moveGroup.ExtendTarget(next);
                    AddFrontBodyToQueue(next);
                }
            }
            
            return moveGroup;
        }
    }

    public class MoveCommand : TurnCommand
    {
        public IReadOnlyList<MoveInfo> MoveInfos => _moveInfos;
        private readonly List<MoveInfo> _moveInfos = new();

        public void AddInfo(MoveInfo moveInfo)
        {
            _moveInfos.Add(moveInfo);
        }
    }
}