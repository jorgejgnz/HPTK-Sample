// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  /// DEPRECATED. Will be removed from headers at version v51.
  public class MatchmakingBrowseResult
  {
    /// DEPRECATED. Will be removed from headers at version v51.
    public readonly MatchmakingEnqueueResult EnqueueResult;
    /// DEPRECATED. Will be removed from headers at version v51.
    public readonly RoomList Rooms;


    public MatchmakingBrowseResult(IntPtr o)
    {
      EnqueueResult = new MatchmakingEnqueueResult(CAPI.ovr_MatchmakingBrowseResult_GetEnqueueResult(o));
      Rooms = new RoomList(CAPI.ovr_MatchmakingBrowseResult_GetRooms(o));
    }
  }

}
