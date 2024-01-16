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
        private Dictionary<int, Vector3> _position;

        private void Awake()
        {
            LevelManager.Instance.RegisterBody(this);
        }
    }
}