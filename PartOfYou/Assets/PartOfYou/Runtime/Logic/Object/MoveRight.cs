using PartOfYou.Runtime.Logic.Level;
using PartOfYou.Runtime.Logic.Properties;

namespace PartOfYou.Runtime.Logic.Object
{
    public class MoveRight : Body, ICanAttachToYou, IUnlockInput
    {
        public InputType UnlockInput => InputType.Right;
        public override bool Movable => true;
    }
}