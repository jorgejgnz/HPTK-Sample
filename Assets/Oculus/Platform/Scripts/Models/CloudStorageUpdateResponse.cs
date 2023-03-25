// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  /// DEPRECATED. Will be removed from headers at version v51.
  public class CloudStorageUpdateResponse
  {
    public readonly string Bucket;
    public readonly string Key;
    public readonly CloudStorageUpdateStatus Status;
    public readonly string VersionHandle;


    public CloudStorageUpdateResponse(IntPtr o)
    {
      Bucket = CAPI.ovr_CloudStorageUpdateResponse_GetBucket(o);
      Key = CAPI.ovr_CloudStorageUpdateResponse_GetKey(o);
      Status = CAPI.ovr_CloudStorageUpdateResponse_GetStatus(o);
      VersionHandle = CAPI.ovr_CloudStorageUpdateResponse_GetVersionHandle(o);
    }
  }

}
