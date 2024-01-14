using PartOfYou.Runtime.Logic.Level;
using PartOfYou.Runtime.Logic.Properties;

namespace PartOfYou.Runtime.Logic.Object
{
    public class MoveLeft : Body, ICanAttachToYou, IUnlockInput
    {
        public InputType UnlockInput => InputType.Left;
        public override bool Movable => true;
    }
}