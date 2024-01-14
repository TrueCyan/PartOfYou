using PartOfYou.Runtime.Logic.Level;
using PartOfYou.Runtime.Logic.Properties;

namespace PartOfYou.Runtime.Logic.Object
{
    public class You : Body, IHaveColor
    {
        public ColorTag ColorTag => ColorTag.White;
        public override bool Movable => true;
    }
}