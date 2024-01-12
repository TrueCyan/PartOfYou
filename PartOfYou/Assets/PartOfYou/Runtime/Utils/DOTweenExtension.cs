using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace PartOfYou.Runtime.Utils
{
    // ReSharper disable once InconsistentNaming
    public static class DOTweenExtension
    {
        // ReSharper disable once InconsistentNaming
        public static TweenerCore<Vector3, Vector3, VectorOptions> DOTranslate(this Transform target,
            Vector3 endValue,
            float duration,
            bool snapping = false)
        {
            return target.DOMove(target.position + endValue, duration, snapping);
        }
    }
}