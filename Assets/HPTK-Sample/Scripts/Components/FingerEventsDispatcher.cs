using HPTK.Views.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static HPTK.Views.Handlers.ProxyHandHandler;

public class FingerEventsDispatcher : MonoBehaviour
{
    public FloatEvent onPinchLerpUpdate;
    public FloatEvent onPinchSpeedUpdate;
    public FloatEvent onBaseRotationUpdate;
    public FloatEvent onFlexUpdate;
    public FloatEvent onStrengthUpdate;
    public FloatEvent onPalmLineUpdate;

    public void UpdateEvents(FingerViewModel finger)
    {
        onPinchLerpUpdate.Invoke(finger.pinchLerp);
        onPinchSpeedUpdate.Invoke(finger.pinchSpeed);
        onBaseRotationUpdate.Invoke(finger.baseRotationLerp);
        onFlexUpdate.Invoke(finger.flexLerp);
        onStrengthUpdate.Invoke(finger.strengthLerp);
        onPalmLineUpdate.Invoke(finger.palmLineLerp);
    }
}
