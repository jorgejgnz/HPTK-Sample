// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  /// DEPRECATED. Will be removed from headers at version v51.
  public class CalApplicationProposed
  {
    public readonly UInt64 ID;


    public CalApplicationProposed(IntPtr o)
    {
      ID = CAPI.ovr_CalApplicationProposed_GetID(o);
    }
  }

}
