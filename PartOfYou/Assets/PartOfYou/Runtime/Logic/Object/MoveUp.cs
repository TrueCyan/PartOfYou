using PartOfYou.Runtime.Logic.Level;
using PartOfYou.Runtime.Logic.Properties;

namespace PartOfYou.Runtime.Logic.Object
{
    public class MoveUp : Body, ICanAttachToYou, IUnlockInput
    {
        public InputType UnlockInput => InputType.Up;
        public override bool Movable => true;
    }
}