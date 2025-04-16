using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;

namespace TuanTool
{
    public static class UniRxExtensions
    {
        ///// <summary>
        ///// Make sure action 100% call with max Time.
        ///// exp:     
        /////     Action action1 = delegate { };
        /////     _showFXAttack(victim, action1);
        /////     UniRxExtensions.MaxTimeForAction(action1, 10, onComplete);
        ///// </summary>
        ///// <param name="actionIn"></param>
        ///// <param name="maxTime"></param>
        ///// <param name="onComplete"></param>
        //public static IDisposable MaxTimeForAction(Action actionIn, float maxTime, Action onComplete)
        //{

        //    var a = Observable.FromEvent(_ => actionIn += _, _ => actionIn -= _).Merge(UniRxExtensions.TimerSecond(maxTime).AsUnitObservable()).FirstOrDefault().Subscribe(delegate
        //         {
        //             Debug.Log($"asdad111");
        //             onComplete?.Invoke();
        //         });
        //    return a;

        //}
        public static IObservable<long> TimerSecond(float second)
        {
            return Observable.Timer(TimeSpan.FromSeconds(second));
        }
    }
}
