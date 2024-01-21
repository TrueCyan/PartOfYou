using System;
using Cysharp.Threading.Tasks;
using PartOfYou.Runtime.Logic.Level;
using PartOfYou.Runtime.Logic.Properties;
using UnityEngine;
using UnityEngine.Playables;

namespace PartOfYou.Runtime.Logic.Object
{
    public class You : Body, IHaveColor
    {
        public ColorTag ColorTag => ColorTag.White;
        public override bool Movable => true;
        [SerializeField] private PlayableDirector _playableDirector;
        [SerializeField] private PlayableAsset _ascend;

        public async UniTask Ascend()
        {
            const double ascendWaitTime = 0.75f;
            _playableDirector.playableAsset = _ascend;
            _playableDirector.Play();
            await UniTask.Delay(TimeSpan.FromSeconds(ascendWaitTime));
        }
    }
}