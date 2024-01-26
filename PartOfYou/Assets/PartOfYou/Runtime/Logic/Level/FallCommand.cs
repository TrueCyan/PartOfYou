using System.Collections.Generic;

namespace PartOfYou.Runtime.Logic.Level
{
    public class FallCommand : TurnCommand
    {
        public List<FallGroup> FallGroups;

        public FallCommand(List<FallGroup> fallGroups)
        {
            FallGroups = fallGroups;
        }
    }
}