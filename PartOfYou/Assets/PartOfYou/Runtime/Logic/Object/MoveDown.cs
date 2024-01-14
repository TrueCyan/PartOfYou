using PartOfYou.Runtime.Logic.Level;
using PartOfYou.Runtime.Logic.Properties;

namespace PartOfYou.Runtime.Logic.Object
{
    public class MoveDown : Body, ICanAttachToYou, IUnlockInput
    {
        public InputType UnlockInput => InputType.Down;
        public override bool Movable => true;
    }
}