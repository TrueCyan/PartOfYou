using System;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.Level
{
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }
        
    public enum TurnAction
    {
        None,
        Do,
        Undo,
        Redo,
        Restart,
    }

    public enum ColorTag
    {
        None,
        White,
    }

    public enum InputType
    {
        Up,
        Down,
        Left,
        Right,
        Undo,
        Redo,
        Restart,
    }

    public static class InLevelTypeConverter
    {
        public static Vector2 DirectionToVector2(Direction direction)
        {
            return direction switch
            {
                Direction.Up => Vector2.up,
                Direction.Down => Vector2.down,
                Direction.Left => Vector2.left,
                Direction.Right => Vector2.right,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
        }
    }
}