using System;
using PartOfYou.Runtime.Logic.Actions;
using PartOfYou.Runtime.Logic.Level;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.Object
{
    public class Sensor : MonoBehaviour
    {
        [SerializeField] private GameAction gameAction;
        
        private void Awake()
        {
            LevelManager.Instance.RegisterSensor(this);
        }

        private bool IsYouInside()
        {
            var body = LevelQuery.GetBodyOnPos(gameObject.transform.position);
            return body != null && body.GetComponent<You>() != null;
        }

        public void TryActivate()
        {
            if (IsYouInside())
            {
                OnActivate();
            }
        }

        private void OnActivate()
        {
            gameAction.Execute();
        }
    }
}