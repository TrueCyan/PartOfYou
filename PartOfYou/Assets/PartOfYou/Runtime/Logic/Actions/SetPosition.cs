using PartOfYou.Runtime.Logic.Object;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.Actions
{
    public class SetPosition : GameAction
    {
        [SerializeField] private Transform position;
        [SerializeField] private Body target;
        

        public override void Execute()
        {
            target.transform.position = position.position;
        }
    }
}