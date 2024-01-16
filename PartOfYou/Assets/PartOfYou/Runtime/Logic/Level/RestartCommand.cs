using System.Collections.Generic;
using PartOfYou.Runtime.Logic.Object;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.Level
{
    public class RestartCommand : TurnCommand
    {
        public readonly List<(Body, Vector3)> PrevPos;
        public readonly List<(Body, Vector3)> NewPos;
        
        public RestartCommand(List<(Body, Vector3)> prevPos, List<(Body, Vector3)> newPos)
        {
            PrevPos = prevPos;
            NewPos = newPos;
        }
    }
}