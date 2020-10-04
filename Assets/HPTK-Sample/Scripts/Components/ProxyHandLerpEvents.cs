using HPTK.Views.Events;
using HPTK.Views.Handlers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ProxyHandLerpEvents : MonoBehaviour
{
    public ProxyHandHandler proxyHand;

    public FloatEvent onErrorLerpUpdate;

    public HandStateEvent onMasterUpdate;
    public HandStateEvent onSlaveUpdate;

    private void Update()
    {
        onErrorLerpUpdate.Invoke(proxyHand.viewModel.errorLerp);

        onMasterUpdate.Invoke(proxyHand.viewModel.master);
        onSlaveUpdate.Invoke(proxyHand.viewModel.slave);
    }
}
