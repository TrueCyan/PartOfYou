using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using PartOfYou.Runtime.Logic.Object;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.Level
{
    public class FallGroup
    {
        private List<Body> _bodies;
        private GameObject _gameObject;

        public FallGroup(List<Body> bodies)
        {
            _bodies = bodies;
        }

        public async UniTask FallAsync(float moveDuration)
        {
            _gameObject = new GameObject
            {
                transform =
                {
                    parent = _bodies[0].transform.parent
                }
            };

            var positionSum = Vector3.zero;
            foreach (var body in _bodies)
            {
                positionSum += body.transform.position;
            }

            var averagePos = positionSum / _bodies.Count;
            _gameObject.transform.position = averagePos;

            foreach (var body in _bodies)
            {
                body.transform.SetParent(_gameObject.transform);
            }

            await DOVirtual.Vector3(Vector3.one, new Vector3(0, 0, 1), moveDuration, x => _gameObject.transform.localScale = x).AsyncWaitForCompletion().AsUniTask();
        }

        public async UniTask Undo(float moveDuration)
        {
            await DOVirtual.Vector3(new Vector3(0, 0, 1), Vector3.one, moveDuration, x => _gameObject.transform.localScale = x).AsyncWaitForCompletion().AsUniTask();
            foreach(var body in _bodies)
            {
                body.transform.parent = _gameObject.transform.parent;
            }

            UnityEngine.Object.Destroy(_gameObject);
        }
    }
}