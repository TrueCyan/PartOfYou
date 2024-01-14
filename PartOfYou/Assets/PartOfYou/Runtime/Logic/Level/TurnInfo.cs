using System.Collections.Generic;

namespace PartOfYou.Runtime.Logic.Level
{
    public class TurnInfo
    {
        public readonly TurnAction Actions;
        public readonly int TurnNumber;

        public TurnInfo(TurnAction type, int turnNumber)
        {
            TurnNumber = turnNumber;
            Actions = type;
        }
    }
}