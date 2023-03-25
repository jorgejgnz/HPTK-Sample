// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  /// DEPRECATED. Will be removed from headers at version v51.
  public class CloudStorageData
  {
    public readonly string Bucket;
    public readonly byte[] Data;
    public readonly uint DataSize;
    public readonly string Key;


    public CloudStorageData(IntPtr o)
    {
      Bucket = CAPI.ovr_CloudStorageData_GetBucket(o);
      Data = CAPI.ovr_CloudStorageData_GetData(o);
      DataSize = CAPI.ovr_CloudStorageData_GetDataSize(o);
      Key = CAPI.ovr_CloudStorageData_GetKey(o);
    }
  }

}
