using System.Collections.Generic;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.Object
{
    public class Body : MonoBehaviour
    {
        public LinkedGroup strongLinkedGroup;
        public List<Body> linkedBodies;

        public bool movable;

        private void Deactivate()
        {
            gameObject.SetActive(false);
        }

        private void Activate()
        {
            gameObject.SetActive(true);
        }
    }
}