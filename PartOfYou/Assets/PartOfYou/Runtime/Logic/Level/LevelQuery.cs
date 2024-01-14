using System.Collections.Generic;
using System.Linq;
using PartOfYou.Runtime.Logic.Object;
using PartOfYou.Runtime.Utils;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.Level
{
    public static class LevelQuery
    {
        private static int ObjectLayer => LayerMask.GetMask("Object");
        private static int FloorLayer => LayerMask.GetMask("Floor");
        private static int GoalLayer => LayerMask.GetMask("Goal");
        private static Body ExistenceCheck(Vector2 position)
        {
            Physics2D.queriesStartInColliders = true;
            var hit = Physics2D.Raycast(position, Vector2.zero, 0, ObjectLayer);
            Physics2D.queriesStartInColliders = false;
            
            if (!hit.collider) return null;
            
            var block = hit.collider.GetComponent<Body>();
            return block;
        }
        
        public static Body GetFrontBody(Body target, Vector2 direction)
        {
            var pos = (Vector2) target.transform.position;
            var front = ExistenceCheck(pos + direction);

            return front;
        }
        
        private static bool FloorCheck(Vector2 position)
        {
            Physics2D.queriesStartInColliders = true;
            var hit = Physics2D.Raycast(position, Vector2.zero, 0, FloorLayer);
            Physics2D.queriesStartInColliders = false;

            return hit.collider.Exists();
        }
        
        public static bool CheckFrontHasFloor(Body target, Vector2 direction)
        {
            var pos = (Vector2) target.transform.position;
            return FloorCheck(pos + direction);
        }
        
        public static IEnumerable<Body> GetNearBody(Body target)
        {
            var pos = (Vector2) target.transform.position;
            var directions = new[]
            {
                InLevelTypeConverter.DirectionToVector2(Direction.Left),
                InLevelTypeConverter.DirectionToVector2(Direction.Right),
                InLevelTypeConverter.DirectionToVector2(Direction.Up),
                InLevelTypeConverter.DirectionToVector2(Direction.Down),
            };

            return directions
                .Select(direction => ExistenceCheck(pos + direction))
                .Where(x => x != null);
        }

        private static bool GoalCheck(Vector2 position)
        {
            Physics2D.queriesStartInColliders = true;
            var hit = Physics2D.Raycast(position, Vector2.zero, 0, GoalLayer);
            Physics2D.queriesStartInColliders = false;
            return hit.collider.Exists();
        }

        public static bool IsBodyOnGoal(Body body)
        {
            var pos = (Vector2)body.transform.position;
            return GoalCheck(pos);
        }
    }
}