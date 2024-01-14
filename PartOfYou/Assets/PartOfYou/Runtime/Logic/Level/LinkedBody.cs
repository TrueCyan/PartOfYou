using System.Collections.Generic;
using PartOfYou.Runtime.Logic.Object;

namespace PartOfYou.Runtime.Logic.Level
{
    public class LinkedBody
    {
        public Body body;
        public List<LinkedBody> linkedBodies;
        
        public LinkedBody(Body body)
        {
            this.body = body;
            linkedBodies = new List<LinkedBody>();
        }

        public void LinkBody(Body target)
        {
            linkedBodies.Add(new LinkedBody(target));
        }
    }
}