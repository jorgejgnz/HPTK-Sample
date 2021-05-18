// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class InviteOptions {

    public InviteOptions() {
      Handle = CAPI.ovr_InviteOptions_Create();
    }


    /// For passing to native C
    public static explicit operator IntPtr(InviteOptions options) {
      return options != null ? options.Handle : IntPtr.Zero;
    }

    ~InviteOptions() {
      CAPI.ovr_InviteOptions_Destroy(Handle);
    }

    IntPtr Handle;
  }
}
