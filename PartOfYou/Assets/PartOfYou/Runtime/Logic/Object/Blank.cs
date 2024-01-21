using PartOfYou.Runtime.Logic.Properties;

namespace PartOfYou.Runtime.Logic.Object
{
    public class Blank : Body, ICanAttachToYou
    {
        public override bool Movable => true;
    }
}