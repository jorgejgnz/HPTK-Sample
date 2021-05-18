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

    /// This the unique API Name that refers to an in-app destination
    public void SetApiName(string value) {
      CAPI.ovr_RichPresenceOptions_SetApiName(Handle, value);
    }

    /// The current amount of users that have joined this user's
    /// squad/team/game/match etc.
    public void SetCurrentCapacity(uint value) {
      CAPI.ovr_RichPresenceOptions_SetCurrentCapacity(Handle, value);
    }

    /// Optionally passed in to use a different deeplink message than the one
    /// defined in the api_name
    public void SetDeeplinkMessageOverride(string value) {
      CAPI.ovr_RichPresenceOptions_SetDeeplinkMessageOverride(Handle, value);
    }

    /// The time the current match/game/round etc. ends
    public void SetEndTime(DateTime value) {
      CAPI.ovr_RichPresenceOptions_SetEndTime(Handle, value);
    }

    public void SetExtraContext(RichPresenceExtraContext value) {
      CAPI.ovr_RichPresenceOptions_SetExtraContext(Handle, value);
    }

    /// Users reported with the same instance ID will be considered to be together
    /// and could interact with each other. Renamed to
    /// RichPresenceOptions.SetInstanceId()
    public void SetInstanceId(string value) {
      CAPI.ovr_RichPresenceOptions_SetInstanceId(Handle, value);
    }

    /// Set whether or not the person is shown as active or idle
    public void SetIsIdle(bool value) {
      CAPI.ovr_RichPresenceOptions_SetIsIdle(Handle, value);
    }

    /// Set whether or not the person is shown as joinable or not to others
    public void SetIsJoinable(bool value) {
      CAPI.ovr_RichPresenceOptions_SetIsJoinable(Handle, value);
    }

    /// The maximum that can join this user
    public void SetMaxCapacity(uint value) {
      CAPI.ovr_RichPresenceOptions_SetMaxCapacity(Handle, value);
    }

    /// The time the current match/game/round etc. started
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
