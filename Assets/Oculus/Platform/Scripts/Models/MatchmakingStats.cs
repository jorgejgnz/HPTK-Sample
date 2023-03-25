// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  /// DEPRECATED. Will be removed from headers at version v51.
  public class MatchmakingStats
  {
    /// DEPRECATED. Will be removed from headers at version v51.
    public readonly uint DrawCount;
    /// DEPRECATED. Will be removed from headers at version v51.
    public readonly uint LossCount;
    /// DEPRECATED. Will be removed from headers at version v51.
    public readonly uint SkillLevel;
    /// DEPRECATED. Will be removed from headers at version v51.
    public readonly double SkillMean;
    /// DEPRECATED. Will be removed from headers at version v51.
    public readonly double SkillStandardDeviation;
    /// DEPRECATED. Will be removed from headers at version v51.
    public readonly uint WinCount;


    public MatchmakingStats(IntPtr o)
    {
      DrawCount = CAPI.ovr_MatchmakingStats_GetDrawCount(o);
      LossCount = CAPI.ovr_MatchmakingStats_GetLossCount(o);
      SkillLevel = CAPI.ovr_MatchmakingStats_GetSkillLevel(o);
      SkillMean = CAPI.ovr_MatchmakingStats_GetSkillMean(o);
      SkillStandardDeviation = CAPI.ovr_MatchmakingStats_GetSkillStandardDeviation(o);
      WinCount = CAPI.ovr_MatchmakingStats_GetWinCount(o);
    }
  }

}
