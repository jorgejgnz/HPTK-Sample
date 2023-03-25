// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class RoomOptions {

    public RoomOptions() {
      Handle = CAPI.ovr_RoomOptions_Create();
    }

    /// DEPRECATED. Will be removed from headers at version v51.
    [Obsolete("Deprecated")]
    public void SetDataStore(string key, string value) {
      CAPI.ovr_RoomOptions_SetDataStoreString(Handle, key, value);
    }

    [Obsolete("Deprecated")]
    public void ClearDataStore() {
      CAPI.ovr_RoomOptions_ClearDataStore(Handle);
    }

    /// DEPRECATED. Will be removed from headers at version v51.
    [Obsolete("Deprecated")]
    public void SetExcludeRecentlyMet(bool value) {
      CAPI.ovr_RoomOptions_SetExcludeRecentlyMet(Handle, value);
    }

    /// DEPRECATED. Will be removed from headers at version v51.
    [Obsolete("Deprecated")]
    public void SetMaxUserResults(uint value) {
      CAPI.ovr_RoomOptions_SetMaxUserResults(Handle, value);
    }

    /// DEPRECATED. Will be removed from headers at version v51.
    [Obsolete("Deprecated")]
    public void SetOrdering(UserOrdering value) {
      CAPI.ovr_RoomOptions_SetOrdering(Handle, value);
    }

    /// DEPRECATED. Will be removed from headers at version v51.
    [Obsolete("Deprecated")]
    public void SetRecentlyMetTimeWindow(TimeWindow value) {
      CAPI.ovr_RoomOptions_SetRecentlyMetTimeWindow(Handle, value);
    }

    /// DEPRECATED. Will be removed from headers at version v51.
    [Obsolete("Deprecated")]
    public void SetRoomId(UInt64 value) {
      CAPI.ovr_RoomOptions_SetRoomId(Handle, value);
    }

    /// DEPRECATED. Will be removed from headers at version v51.
    [Obsolete("Deprecated")]
    public void SetTurnOffUpdates(bool value) {
      CAPI.ovr_RoomOptions_SetTurnOffUpdates(Handle, value);
    }


    /// For passing to native C
    public static explicit operator IntPtr(RoomOptions options) {
      return options != null ? options.Handle : IntPtr.Zero;
    }

    ~RoomOptions() {
      CAPI.ovr_RoomOptions_Destroy(Handle);
    }

    IntPtr Handle;
  }
}
