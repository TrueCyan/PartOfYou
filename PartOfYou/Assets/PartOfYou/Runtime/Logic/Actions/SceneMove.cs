using Cysharp.Threading.Tasks;
using PartOfYou.Runtime.Logic.System;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.Actions
{
    public class SceneMove : GameAction
    {
        [SerializeField] private string moveToScene;

        public override void Execute()
        {
            GameManager.Instance.transition.SceneMove(moveToScene).Forget();
        }
    }
}