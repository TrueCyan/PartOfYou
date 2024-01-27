using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using PartOfYou.Runtime.Logic.Object;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.Level
{
    public class FallGroup
    {
        private readonly List<Body> _bodies;
        private GameObject _gameObject;
        private readonly List<(Body, Vector3)> _positionSnapshot;

        public FallGroup(List<Body> bodies)
        {
            _bodies = bodies;
            _positionSnapshot = bodies.Select(x => (x, x.transform.position)).ToList();
        }

        public async UniTask FallAsync(float moveDuration)
        {
            Pack();
            await DOVirtual.Vector3(Vector3.one, new Vector3(0, 0, 1), moveDuration, x => _gameObject.transform.localScale = x).AsyncWaitForCompletion().AsUniTask();
        }

        public async UniTask Undo(float moveDuration)
        {
            await DOVirtual.Vector3(new Vector3(0, 0, 1), Vector3.one, moveDuration, x => _gameObject.transform.localScale = x).AsyncWaitForCompletion().AsUniTask();
            Unpack();
        }

        public void Unpack()
        {
            _gameObject.transform.localScale = Vector3.one;
            foreach(var body in _bodies)
            {
                body.transform.parent = _gameObject.transform.parent;
            }

            UnityEngine.Object.Destroy(_gameObject);
        }

        private void Pack()
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
        }

        public void Repack()
        {
            foreach (var pair in _positionSnapshot)
            {
                var (body, position) = pair; 
                body.transform.position = position;
            }

            Pack();
            _gameObject.transform.localScale = new Vector3(0, 0, 1);
        }
    }
}