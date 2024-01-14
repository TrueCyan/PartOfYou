using System.Collections.Generic;
using System.Linq;
using PartOfYou.Runtime.Logic.Object;
using PartOfYou.Runtime.Utils;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.Level
{
    public class MoveGroup
    {
        public readonly List<Body> GroupedBodies;
        
        public readonly Direction MoveDirection;

        public bool Movable = true;
        
        public IEnumerable<Transform> GetTransforms => GroupedBodies.Select(block => block.transform);

        public MoveGroup(Direction direction)
        {
            MoveDirection = direction;
            GroupedBodies = new List<Body>();
        }

        private void ExtendGroup(Body body)
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

        public void MergeGroup(MoveGroup moveGroup)
        {
            foreach (var body in moveGroup.GroupedBodies)
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
        
        public static MoveGroup GetGroup(Body body, Direction direction)
        {
            var moveGroup = new MoveGroup(direction);
            
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
                        moveGroup.ExtendGroup(member);

                        if (member.isActiveAndEnabled)
                        {
                            AddFrontBodyToQueue(member);
                        }
                    }
                }
                else
                {
                    moveGroup.ExtendGroup(next);
                    AddFrontBodyToQueue(next);
                }
            }
            
            return moveGroup;
        }
    }
}