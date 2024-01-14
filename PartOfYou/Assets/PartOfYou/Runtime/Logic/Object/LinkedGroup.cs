using System.Collections.Generic;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.Object
{
    public class LinkedGroup : MonoBehaviour
    {
        public List<Body> Members { get; } = new List<Body>();
    }
}