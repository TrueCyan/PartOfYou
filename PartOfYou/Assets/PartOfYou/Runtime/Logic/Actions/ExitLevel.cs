using PartOfYou.Runtime.Logic.Level;

namespace PartOfYou.Runtime.Logic.Actions
{
    public class ExitLevel : GameAction
    {
        public override void Execute()
        {
            LevelManager.Instance.ExitLevel();
        }
    }
}