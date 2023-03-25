// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  /// DEPRECATED. Will be removed from headers at version v51.
  public class MatchmakingEnqueueResultAndRoom
  {
    /// DEPRECATED. Will be removed from headers at version v51.
    public readonly MatchmakingEnqueueResult MatchmakingEnqueueResult;
    /// DEPRECATED. Will be removed from headers at version v51.
    public readonly Room Room;


    public MatchmakingEnqueueResultAndRoom(IntPtr o)
    {
      MatchmakingEnqueueResult = new MatchmakingEnqueueResult(CAPI.ovr_MatchmakingEnqueueResultAndRoom_GetMatchmakingEnqueueResult(o));
      Room = new Room(CAPI.ovr_MatchmakingEnqueueResultAndRoom_GetRoom(o));
    }
  }

}
