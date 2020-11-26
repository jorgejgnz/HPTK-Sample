using HPTK.Views.Handlers;
using HPTK.Views.Handlers.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProxyHandViewModelInspector : MonoBehaviour
{
    public ProxyHandHandler handler;

    public void SetMasterHandEnabled(bool enabled)
    {
        handler.viewModel.SetMasterActive(enabled);
    }

    public void SetSlaveHandEnabled(bool enabled)
    {
        handler.viewModel.SetSlaveActive(enabled);
    }
}
