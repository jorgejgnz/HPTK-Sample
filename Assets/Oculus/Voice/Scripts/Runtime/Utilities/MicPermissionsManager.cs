/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Oculus.VoiceSDK.Utilities
{
    public class MicPermissionsManager
    {
        public static bool HasMicPermission()
        {
#if UNITY_ANDROID
            return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#else
            return true;
#endif
        }
    
        public static void RequestMicPermission(Action<string> permissionGrantedCallback = null)
        {
#if UNITY_ANDROID
            if (HasMicPermission())
            {
                permissionGrantedCallback?.Invoke(Permission.Microphone);
                return;
            }
            
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += s => permissionGrantedCallback?.Invoke(s);
            Permission.RequestUserPermission(Permission.Microphone, callbacks);
#else
            permissionGrantedCallback?.Invoke("android.permission.RECORD_AUDIO");

            // Do nothing for now, but eventually we may want to handle IOS/whatever permissions here, too.
#endif
        }
    }
}
