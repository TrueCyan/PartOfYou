namespace PartOfYou.Runtime.Logic.Level {
    public class Input
    {
        
    }

    public class Move : Input
    {
        public Direction Direction;

        public Move(Direction direction)
        {
            Direction = direction;
        }
    }

    public class Undo : Input
    {
        
    }

    public class Redo : Input
    {
        
    }

    public class Restart : Input
    {
        
    }
}