using HPTK.Views.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static HPTK.Views.Handlers.ProxyHandHandler;

public class HandEventsDispatcher : MonoBehaviour
{    
    public FloatEvent onFistLerpUpdate;
    public FloatEvent onGraspLerpUpdate;
    public FloatEvent onGraspSpeedUpdate;

    [Header("Fingers")]
    public FingerStateEvent onThumbUpdate;
    public FingerStateEvent onIndexUpdate;
    public FingerStateEvent onMiddleUpdate;
    public FingerStateEvent onRingUpdate;
    public FingerStateEvent onPinkyUpdate;

    public void UpdateEvents(HandViewModel hand)
    {
        onFistLerpUpdate.Invoke(hand.fistLerp);
        onGraspLerpUpdate.Invoke(hand.graspLerp);
        onGraspSpeedUpdate.Invoke(hand.graspSpeed);

        onThumbUpdate.Invoke(hand.thumb);
        onIndexUpdate.Invoke(hand.index);
        onMiddleUpdate.Invoke(hand.middle);
        onRingUpdate.Invoke(hand.ring);
        onPinkyUpdate.Invoke(hand.pinky);
    }
}
