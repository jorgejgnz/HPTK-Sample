// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class ApplicationVersion
  {
    public readonly int CurrentCode;
    public readonly string CurrentName;
    public readonly int LatestCode;
    public readonly string LatestName;
    /// Seconds since epoch when the latest app update was released
    public readonly long ReleaseDate;
    /// Size of the app update in bytes
    public readonly string Size;


    public ApplicationVersion(IntPtr o)
    {
      CurrentCode = CAPI.ovr_ApplicationVersion_GetCurrentCode(o);
      CurrentName = CAPI.ovr_ApplicationVersion_GetCurrentName(o);
      LatestCode = CAPI.ovr_ApplicationVersion_GetLatestCode(o);
      LatestName = CAPI.ovr_ApplicationVersion_GetLatestName(o);
      ReleaseDate = CAPI.ovr_ApplicationVersion_GetReleaseDate(o);
      Size = CAPI.ovr_ApplicationVersion_GetSize(o);
    }
  }

}
