using System;
using System.Collections.Generic;
using PartOfYou.Runtime.Logic.Level;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.Object
{
    public abstract class Body : MonoBehaviour
    {
        public LinkedGroup strongLinkedGroup;
        public List<Body> linkedBodies;

        public bool movable;

        private void Awake()
        {
            LevelManager.Instance.RegisterBody(this);
        }
    }
}