// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

#pragma warning disable 0618

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  /// DEPRECATED. Will be removed from headers at version v51.
  public class MatchmakingEnqueueResult
  {
    /// DEPRECATED. Will be removed from headers at version v51.
    ///
    /// If 'IsDebug' is set in ovrMatchmakingOptionsHandle, this will return with
    /// the enqueue results.
    // May be null. Check before using.
    public readonly MatchmakingAdminSnapshot AdminSnapshotOptional;
    [Obsolete("Deprecated in favor of AdminSnapshotOptional")]
    public readonly MatchmakingAdminSnapshot AdminSnapshot;
    /// DEPRECATED. Will be removed from headers at version v51.
    ///
    /// The average amount of time (mean average) that users in this queue have
    /// waited during the last hour or more. The wait times, whether the users
    /// canceled or found a match, are used to generate this value. Use this to
    /// give users an indication of how long they can expect to wait.
    public readonly uint AverageWait;
    /// DEPRECATED. Will be removed from headers at version v51.
    ///
    /// The number of matches made from the pool the user is participating in. You
    /// can use this to give users an indication of whether they should bother to
    /// wait.
    public readonly uint MatchesInLastHourCount;
    /// DEPRECATED. Will be removed from headers at version v51.
    ///
    /// The amount of time the 95th percentile waited during the last hour or more.
    /// The wait times, whether the users canceled or found a match, are used to
    /// generate this value. Use this to give users an indication of the maximum
    /// amount of time they can expect to wait.
    public readonly uint MaxExpectedWait;
    /// DEPRECATED. Will be removed from headers at version v51.
    public readonly string Pool;
    /// DEPRECATED. Will be removed from headers at version v51.
    ///
    /// Percentage of people in the same queue as the user who got matched, from 0
    /// to 100 percent. Stats are taken from the last hour or more. You can use
    /// this to give users an indication of whether they should wait.
    public readonly uint RecentMatchPercentage;
    /// DEPRECATED. Will be removed from headers at version v51.
    public readonly string RequestHash;


    public MatchmakingEnqueueResult(IntPtr o)
    {
      {
        var pointer = CAPI.ovr_MatchmakingEnqueueResult_GetAdminSnapshot(o);
        AdminSnapshot = new MatchmakingAdminSnapshot(pointer);
        if (pointer == IntPtr.Zero) {
          AdminSnapshotOptional = null;
        } else {
          AdminSnapshotOptional = AdminSnapshot;
        }
      }
      AverageWait = CAPI.ovr_MatchmakingEnqueueResult_GetAverageWait(o);
      MatchesInLastHourCount = CAPI.ovr_MatchmakingEnqueueResult_GetMatchesInLastHourCount(o);
      MaxExpectedWait = CAPI.ovr_MatchmakingEnqueueResult_GetMaxExpectedWait(o);
      Pool = CAPI.ovr_MatchmakingEnqueueResult_GetPool(o);
      RecentMatchPercentage = CAPI.ovr_MatchmakingEnqueueResult_GetRecentMatchPercentage(o);
      RequestHash = CAPI.ovr_MatchmakingEnqueueResult_GetRequestHash(o);
    }
  }

}
