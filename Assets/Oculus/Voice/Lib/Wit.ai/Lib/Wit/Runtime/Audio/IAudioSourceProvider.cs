/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEngine;

namespace Meta.Voice.Audio
{
    /// <summary>
    /// A simple interface for receiving a Unity AudioSource
    /// </summary>
    public interface IAudioSourceProvider
    {
        AudioSource AudioSource { get; }
    }
}
