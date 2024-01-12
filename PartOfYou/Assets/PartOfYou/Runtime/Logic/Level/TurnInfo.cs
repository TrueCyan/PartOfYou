namespace PartOfYou.Runtime.Logic.Level
{
    public class TurnInfo
    {
        public readonly Turn Type;
        public readonly int TurnNumber;

        public TurnInfo(Turn type, int turnNumber)
        {
            Type = type;
            TurnNumber = turnNumber;
        }
    }
}