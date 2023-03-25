// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  /// DEPRECATED. Will be removed from headers at version v51.
  public class Team
  {
    /// DEPRECATED. Will be removed from headers at version v51.
    public readonly UserList AssignedUsers;
    /// DEPRECATED. Will be removed from headers at version v51.
    public readonly int MaxUsers;
    /// DEPRECATED. Will be removed from headers at version v51.
    public readonly int MinUsers;
    /// DEPRECATED. Will be removed from headers at version v51.
    public readonly string Name;


    public Team(IntPtr o)
    {
      AssignedUsers = new UserList(CAPI.ovr_Team_GetAssignedUsers(o));
      MaxUsers = CAPI.ovr_Team_GetMaxUsers(o);
      MinUsers = CAPI.ovr_Team_GetMinUsers(o);
      Name = CAPI.ovr_Team_GetName(o);
    }
  }

  public class TeamList : DeserializableList<Team> {
    public TeamList(IntPtr a) {
      var count = (int)CAPI.ovr_TeamArray_GetSize(a);
      _Data = new List<Team>(count);
      for (int i = 0; i < count; i++) {
        _Data.Add(new Team(CAPI.ovr_TeamArray_GetElement(a, (UIntPtr)i)));
      }

    }

  }
}
