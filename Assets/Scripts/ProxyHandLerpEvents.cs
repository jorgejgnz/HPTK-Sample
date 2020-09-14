using HPTK.Views.Events;
using HPTK.Views.Handlers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ProxyHandLerpEvents : MonoBehaviour
{
    public ProxyHandHandler proxyHand;

    public FloatEvent onGraspLerpUpdate;
    public FloatEvent onIndexPinchLerpUpdate;
    public FloatEvent onErrorLerpUpdate;

    private void Update()
    {
        onGraspLerpUpdate.Invoke(proxyHand.viewModel.graspLerp);
        onIndexPinchLerpUpdate.Invoke(proxyHand.viewModel.indexPinchLerp);
        onErrorLerpUpdate.Invoke(proxyHand.viewModel.errorLerp);
    }
}
