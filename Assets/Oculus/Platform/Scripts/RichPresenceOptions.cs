// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class RichPresenceOptions {

    public RichPresenceOptions() {
      Handle = CAPI.ovr_RichPresenceOptions_Create();
    }

    /// DEPRECATED. Use GroupPresenceOptions.SetDestinationApiName()
    [Obsolete("Deprecated")]
    public void SetApiName(string value) {
      CAPI.ovr_RichPresenceOptions_SetApiName(Handle, value);
    }

    /// DEPRECATED. Unused. Will be removed from headers at version v51.
    [Obsolete("Deprecated")]
    public void SetCurrentCapacity(uint value) {
      CAPI.ovr_RichPresenceOptions_SetCurrentCapacity(Handle, value);
    }

    /// DEPRECATED. Use GroupPresenceOptions.SetDeeplinkMessageOverride()
    [Obsolete("Deprecated")]
    public void SetDeeplinkMessageOverride(string value) {
      CAPI.ovr_RichPresenceOptions_SetDeeplinkMessageOverride(Handle, value);
    }

    /// DEPRECATED. Unused. Will be removed from headers at version v51.
    [Obsolete("Deprecated")]
    public void SetEndTime(DateTime value) {
      CAPI.ovr_RichPresenceOptions_SetEndTime(Handle, value);
    }

    /// DEPRECATED. Unused. Will be removed from headers at version v51.
    [Obsolete("Deprecated")]
    public void SetExtraContext(RichPresenceExtraContext value) {
      CAPI.ovr_RichPresenceOptions_SetExtraContext(Handle, value);
    }

    /// DEPRECATED. Use GroupPresenceOptions.SetMatchSessionId() Will be removed
    /// from headers at version v51.
    [Obsolete("Deprecated")]
    public void SetInstanceId(string value) {
      CAPI.ovr_RichPresenceOptions_SetInstanceId(Handle, value);
    }

    /// DEPRECATED. Unused. Will be removed from headers at version v51.
    [Obsolete("Deprecated")]
    public void SetIsIdle(bool value) {
      CAPI.ovr_RichPresenceOptions_SetIsIdle(Handle, value);
    }

    /// DEPRECATED. Use GroupPresenceOptions.SetIsJoinable()
    [Obsolete("Deprecated")]
    public void SetIsJoinable(bool value) {
      CAPI.ovr_RichPresenceOptions_SetIsJoinable(Handle, value);
    }

    /// DEPRECATED. Unused. Will be removed from headers at version v51.
    [Obsolete("Deprecated")]
    public void SetMaxCapacity(uint value) {
      CAPI.ovr_RichPresenceOptions_SetMaxCapacity(Handle, value);
    }

    /// DEPRECATED. Unused. Will be removed from headers at version v51.
    [Obsolete("Deprecated")]
    public void SetStartTime(DateTime value) {
      CAPI.ovr_RichPresenceOptions_SetStartTime(Handle, value);
    }


    /// For passing to native C
    public static explicit operator IntPtr(RichPresenceOptions options) {
      return options != null ? options.Handle : IntPtr.Zero;
    }

    ~RichPresenceOptions() {
      CAPI.ovr_RichPresenceOptions_Destroy(Handle);
    }

    IntPtr Handle;
  }
}
