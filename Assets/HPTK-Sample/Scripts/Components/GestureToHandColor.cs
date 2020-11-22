using HPTK.Views.Handlers;
using UnityEngine;
using static HPTK.Views.Handlers.ProxyHandHandler;

public class GestureToHandColor : MonoBehaviour
{
    public AvatarHandler avatar;

    private void Update()
    {
        UpdateHandColor(avatar.viewModel.leftHand.viewModel.slave);
        UpdateHandColor(avatar.viewModel.rightHand.viewModel.slave);
    }

    void UpdateHandColor(HandViewModel hand)
    {
        Color c = new Color();
        c.r = hand.graspLerp;
        c.b = hand.index.pinchLerp;
        hand.skinnedMR.material.SetColor("_BaseColor",c);
    }
}
