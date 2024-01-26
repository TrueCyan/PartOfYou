using PartOfYou.Runtime.Logic.Level;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.Object
{
    public class Hole : MonoBehaviour
    {
        private void Awake()
        {
            LevelManager.Instance.RegisterHole(this);
        }
    }
}