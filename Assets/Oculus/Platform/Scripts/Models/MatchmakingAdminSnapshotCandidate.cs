// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  /// DEPRECATED. Will be removed from headers at version v51.
  public class MatchmakingAdminSnapshotCandidate
  {
    /// DEPRECATED. Will be removed from headers at version v51.
    public readonly bool CanMatch;
    /// DEPRECATED. Will be removed from headers at version v51.
    public readonly double MyTotalScore;
    /// DEPRECATED. Will be removed from headers at version v51.
    public readonly double TheirCurrentThreshold;
    /// DEPRECATED. Will be removed from headers at version v51.
    public readonly double TheirTotalScore;
    /// DEPRECATED. Will be removed from headers at version v51.
    public readonly string TraceId;


    public MatchmakingAdminSnapshotCandidate(IntPtr o)
    {
      CanMatch = CAPI.ovr_MatchmakingAdminSnapshotCandidate_GetCanMatch(o);
      MyTotalScore = CAPI.ovr_MatchmakingAdminSnapshotCandidate_GetMyTotalScore(o);
      TheirCurrentThreshold = CAPI.ovr_MatchmakingAdminSnapshotCandidate_GetTheirCurrentThreshold(o);
      TheirTotalScore = CAPI.ovr_MatchmakingAdminSnapshotCandidate_GetTheirTotalScore(o);
      TraceId = CAPI.ovr_MatchmakingAdminSnapshotCandidate_GetTraceId(o);
    }
  }

  public class MatchmakingAdminSnapshotCandidateList : DeserializableList<MatchmakingAdminSnapshotCandidate> {
    public MatchmakingAdminSnapshotCandidateList(IntPtr a) {
      var count = (int)CAPI.ovr_MatchmakingAdminSnapshotCandidateArray_GetSize(a);
      _Data = new List<MatchmakingAdminSnapshotCandidate>(count);
      for (int i = 0; i < count; i++) {
        _Data.Add(new MatchmakingAdminSnapshotCandidate(CAPI.ovr_MatchmakingAdminSnapshotCandidateArray_GetElement(a, (UIntPtr)i)));
      }

    }

  }
}
