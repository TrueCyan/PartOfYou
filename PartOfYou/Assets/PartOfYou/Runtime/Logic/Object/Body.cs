using System;
using System.Collections.Generic;
using PartOfYou.Runtime.Logic.Level;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.Object
{
    public abstract class Body : MonoBehaviour
    {
        [HideInInspector] public LinkedGroup strongLinkedGroup;
        public abstract bool Movable { get; }

        private void Awake()
        {
            LevelManager.Instance.RegisterBody(this);
        }
    }
}