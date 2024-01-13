using System.Collections.Generic;
using System.Linq;
using PartOfYou.Runtime.Logic.Object;
using PartOfYou.Runtime.Utils;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.Level
{
    public class MoveGroup
    {
        private readonly List<Body> _groupedBodies;
        
        public readonly Direction MoveDirection;

        public bool Movable = true;
        
        private static int ObjectLayer => LayerMask.GetMask("Object");
        
        public IEnumerable<Transform> GetTransforms => _groupedBodies.Select(block => block.transform);

        public MoveGroup(Direction direction)
        {
            MoveDirection = direction;
            _groupedBodies = new List<Body>();
        }

        private void ExtendGroup(Body body)
        {
            if (!_groupedBodies.Contains(body))
            {
                _groupedBodies.Add(body);
            }
            else
            {
                Debug.LogError("[ActionManager.cs] !! Trying to add same Body to the MoveGroup !!");
            }
        }

        public void MergeGroup(MoveGroup moveGroup)
        {
            foreach (var body in moveGroup._groupedBodies)
            {
                if (!_groupedBodies.Contains(body))
                {
                    _groupedBodies.Add(body);
                }
            }
        }

        private bool Contains(Body body)
        {
            return _groupedBodies.Contains(body);
        }
        
        private static Body ExistenceCheck(Vector2 position)
        {
            Physics2D.queriesStartInColliders = true;
            var hit = Physics2D.Raycast(position, Vector2.zero, 0, ObjectLayer);
            Physics2D.queriesStartInColliders = false;
            
            if (!hit.collider) return null;
            
            var block = hit.collider.GetComponent<Body>();
            return block;
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
                var pos = (Vector2) target.transform.position;
                var front = ExistenceCheck(pos + dir);
                if (front.Exists() && !moveGroup.Contains(front))
                {
                    queue.Enqueue(front);
                }
            }
            
            while (queue.Count > 0)
            {
                var next = queue.Dequeue();
                
                if (moveGroup.Contains(next)) continue;
                if (!next.movable)
                {
                    moveGroup.Movable = false;
                    break;
                }

                foreach (var linkedBody in next.linkedBodies)
                {
                    var linkedGroup = GetGroup(linkedBody, direction);
                    if (linkedBody.movable)
                    {
                        moveGroup.MergeGroup(linkedGroup);
                    }
                }
                
                var group = next.strongLinkedGroup;
                if (group)
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