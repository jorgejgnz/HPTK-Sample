// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  /// DEPRECATED. Will be removed from headers at version v51.
  public class CloudStorageConflictMetadata
  {
    public readonly CloudStorageMetadata Local;
    public readonly CloudStorageMetadata Remote;


    public CloudStorageConflictMetadata(IntPtr o)
    {
      Local = new CloudStorageMetadata(CAPI.ovr_CloudStorageConflictMetadata_GetLocal(o));
      Remote = new CloudStorageMetadata(CAPI.ovr_CloudStorageConflictMetadata_GetRemote(o));
    }
  }

}
