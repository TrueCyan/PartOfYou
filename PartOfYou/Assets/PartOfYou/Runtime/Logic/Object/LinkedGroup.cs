using System.Collections.Generic;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.Object
{
    public class LinkedGroup : MonoBehaviour
    {
        public List<Body> Members { get; } = new List<Body>();

        public void AddToGroup(Body body)
        {
            Members.Add(body);
        }

        public void RemoveFromGroup(Body body)
        {
            Members.Remove(body);
        }
    }
}