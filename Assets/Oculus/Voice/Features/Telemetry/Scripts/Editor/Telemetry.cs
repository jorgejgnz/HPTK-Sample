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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace Meta.Voice.TelemetryUtilities
{
    /// <summary>
    /// This class provides facilities to share telemetry from Unity Editor with Meta.
    /// </summary>
    [ExecuteInEditMode]
    internal class Telemetry : MonoBehaviour
    {
        /// <summary>
        /// The editor prefs key holding the logging level.
        /// </summary>
        private const string TELEMETRY_LOGGING_LEVEL = "VoiceSdk.Telemetry.LogLevel";

        /// <summary>
        /// The editor prefs key holding a unique identifier that is set once when consent is obtained and never changes.
        /// This is an approximation of a device ID but is tied to the Unity editor prefs instead, so can change.
        /// </summary>
        private const string TELEMETRY_ENV_ID = "VoiceSdk.Telemetry.EnvironmentId";

        /// <summary>
        /// The active telemetry channel. Used to switch between live and local.
        /// </summary>
        private static TelemetryChannel _instance = new LocalTelemetry();

        /// <summary>
        /// Holds the current status of use consent to share telemetry.
        /// </summary>
        private static bool _consentProvided = false;

        /// <summary>
        /// Sequential counter to use as unique instance keys.
        /// </summary>
        private static int _nextEventSequenceId = 1;

        /// <summary>
        /// The ID of the entire session event.
        /// </summary>
        private static int _sessionEventInstanceId;

        /// <summary>
        /// An ID that is generated once and stored in the preferences. It's similar to a device ID, except it will change
        /// any time it's regenerated or Unity editor preferences are removed.
        /// </summary>
        private static Guid _envId;

        /// <summary>
        /// A random ID given to the session to use for correlation.
        /// </summary>
        private static Guid _sessionID = Guid.NewGuid();

        static Telemetry()
        {
            #if UNITY_EDITOR_WIN
            ConsentProvided = TelemetryConsentManager.ConsentProvided;
            #endif
        }

        internal static TelemetryLogLevel LogLevel
        {
            get
            {
                if (_logLevel != TelemetryLogLevel.Unassigned)
                {
                    return _logLevel;
                }

                if (!EditorPrefs.HasKey(TELEMETRY_LOGGING_LEVEL))
                {
                    _logLevel = TelemetryLogLevel.Off;
                }
                else
                {
                    var telemetryLevelString = EditorPrefs.GetString(Telemetry.TELEMETRY_LOGGING_LEVEL);
                    Enum.TryParse(telemetryLevelString, true,
                        out TelemetryLogLevel telemetryLogLevel);
                    _logLevel = telemetryLogLevel;
                }

                return _logLevel;
            }
            set
            {
                EditorPrefs.SetString(Telemetry.TELEMETRY_LOGGING_LEVEL, value.ToString());
                _logLevel = value;
            }
        }

        private static TelemetryLogLevel _logLevel = TelemetryLogLevel.Unassigned;

        /// <summary>
        /// Sets consent as obtained or withdrawn. This controls whether Meta will collect telemetry or not
        /// </summary>
        internal static bool ConsentProvided
        {
            set
            {
                if (_consentProvided == value)
                {
                    return;
                }

                _consentProvided = value;

                if (value)
                {
                    ExtractEnvironmentId();

                    _instance = new TelemetryChannel();

                    _sessionEventInstanceId = StartEvent(TelemetryEventId.Session);
                }
                else
                {
                    _instance = new LocalTelemetry();
                }
            }
        }

        /// <summary>
        /// Logs an event as started.
        /// </summary>
        /// <param name="eventId">The ID of the event that just started.</param>
        /// <returns>Instance key identifying this specific occurrence of the event.</returns>
        internal static int StartEvent(TelemetryEventId eventId)
        {
            return _instance.StartEvent(eventId);
        }

        /// <summary>
        /// Logs an instantaneous event that happens at one point in time (as opposed to one with a start and end).
        /// </summary>
        /// <param name="eventId">The ID of the event.</param>
        /// <param name="annotations">Optional annotations to add to the event.</param>
        /// <returns>Instance key identifying this specific occurrence of the event.</returns>
        internal static void LogInstantEvent(TelemetryEventId eventId,
            Dictionary<AnnotationKey, string> annotations = null)
        {
            _instance.LogInstantEvent(eventId, annotations);
        }

        /// <summary>
        /// Annotates an event that has started but did not end yet.
        /// </summary>
        /// <param name="instanceKey">The instance key of the event. Must match the one used starting the event.</param>
        /// <param name="key">The annotation key.</param>
        /// <param name="value">The annotation value.</param>
        internal static void AnnotateEvent(int instanceKey, AnnotationKey key,
            string value)
        {
            _instance.AnnotateEvent(instanceKey, key, value);
        }

        /// <summary>
        /// Logs an event as ended.
        /// </summary>
        /// <param name="instanceKey">The instance key of the event. Must match the one used starting the event.</param>
        /// <param name="result">The result of the event.</param>
        /// <returns>Instance key.</returns>
        internal static void EndEvent(int instanceKey, ResultType result)
        {
            _instance.EndEvent(instanceKey, result);
        }

        /// <summary>
        /// Logs an event as ended.
        /// </summary>
        /// <param name="instanceKey">The instance key of the event. Must match the one used starting the event.</param>
        /// <param name="error">The error.</param>
        /// <returns>Instance key.</returns>
        internal static void EndEventWithFailure(int instanceKey, string error = null)
        {
            if (!string.IsNullOrEmpty(error))
            {
                _instance.AnnotateEvent(instanceKey, AnnotationKey.Error, error);
            }

            _instance.EndEvent(instanceKey, ResultType.Failure);
        }

        private static void ExtractEnvironmentId()
        {
            var keyExists = EditorPrefs.HasKey(TELEMETRY_ENV_ID);
            if (!keyExists)
            {
                _envId = Guid.NewGuid();
                EditorPrefs.SetString(TELEMETRY_ENV_ID, _envId.ToString());
            }
            else
            {
                var idString = EditorPrefs.GetString(TELEMETRY_ENV_ID);
                if (!Guid.TryParse(idString, out _envId))
                {
                    LogWarning($"Failed to parse telemetry environment ID from: {idString}");
                }
            }
        }

        private static void LogWarning(object content)
        {
            if (_logLevel == TelemetryLogLevel.Off)
            {
                return;
            }

            Debug.LogWarning(content);
        }

        private static void LogVerbose(object content)
        {
            if (_logLevel != TelemetryLogLevel.Verbose)
            {
                return;
            }

            Debug.Log(content);
        }

        private static void LogError(object content)
        {
            if (_logLevel == TelemetryLogLevel.Off)
            {
                return;
            }

            Debug.LogError(content);
        }

        private void OnDestroy()
        {
            EndEvent(_sessionEventInstanceId, ResultType.Success);
            _instance.ShutdownTelemetry();
        }

        /// <summary>
        /// The telemetry channel represents a target for telemetry. The default implementation sends live telemetry
        /// to Meta.
        /// </summary>
        private class TelemetryChannel
        {
            private const string PluginName = "SDKTelemetry";

            /// <summary>
            /// Maps the instance keys with their corresponding events.
            /// </summary>
            private Dictionary<int, TelemetryEventId> _instanceKeyMap = new Dictionary<int, TelemetryEventId>();

            /// <summary>
            /// Logs an event as started.
            /// </summary>
            /// <param name="eventId">The ID of the event that just started.</param>
            /// <returns>Instance key identifying this specific occurrence of the event.</returns>
            internal virtual int StartEvent(TelemetryEventId eventId)
            {
                _instanceKeyMap[_nextEventSequenceId] = eventId;
                QplMarkerStart((int)eventId, _nextEventSequenceId, -1);
                AnnotateEvent(_nextEventSequenceId, AnnotationKey.SessionId, _sessionID.ToString());
                AnnotateEvent(_nextEventSequenceId, AnnotationKey.StartTimeStamp,
                    ElapsedMilliseconds.ToString());

                LogVerbose($"Started telemetry event {eventId}:{_nextEventSequenceId}");
                return _nextEventSequenceId++;
            }

            /// <summary>
            /// Logs an instantaneous event that happens at one point in time (as opposed to one with a start and end).
            /// </summary>
            /// <param name="eventId">The ID of the event.</param>
            /// <param name="annotations">Optional annotations to add to the event.</param>
            /// <returns>Instance key identifying this specific occurrence of the event.</returns>
            internal virtual void LogInstantEvent(TelemetryEventId eventId,
                Dictionary<AnnotationKey, string> annotations = null)
            {
                var instanceKey = _nextEventSequenceId;
                _instanceKeyMap[instanceKey] = eventId;
                var timeStamp = ElapsedMilliseconds.ToString();
                QplMarkerStart((int)eventId, instanceKey, -1);
                LogVerbose($"Started instant telemetry event {eventId}:{instanceKey}");
                AnnotateEvent(instanceKey, AnnotationKey.SessionId, _sessionID.ToString());
                AnnotateEvent(instanceKey, AnnotationKey.StartTimeStamp, timeStamp);
                AnnotateEvent(instanceKey, AnnotationKey.EndTimeStamp, timeStamp);

                if (annotations != null)
                {
                    foreach (var annotation in annotations)
                    {
                        AnnotateEvent(instanceKey, annotation.Key, annotation.Value);
                    }
                }

                QplMarkerEnd((int)eventId, ResultType.Success, instanceKey, -1);
                LogVerbose($"Ended instant telemetry event {eventId}:{instanceKey}");
                _instanceKeyMap.Remove(instanceKey);
                ++_nextEventSequenceId;
            }

            /// <summary>
            /// Annotates an event that has started but did not end yet.
            /// </summary>
            /// <param name="eventId">The ID of the event. Should already have started but not ended already.</param>
            /// <param name="instanceKey">The instance key of the event. Must match the one used starting the event.</param>
            /// <param name="key">The annotation key.</param>
            /// <param name="value">The annotation value.</param>
            internal virtual void AnnotateEvent(int instanceKey, AnnotationKey key,
                string value)
            {
                if (!this._instanceKeyMap.ContainsKey(instanceKey))
                {
                    LogWarning($"Attempted to end an event that's invalid or not started. Instance ID: {instanceKey}");
                    return;
                }

                var eventId = _instanceKeyMap[instanceKey];
                QplMarkerAnnotation((int)eventId, key.ToString(), value, instanceKey);
                LogVerbose($"Annotated telemetry event {eventId}:{instanceKey} with {key}:{value}");
            }

            /// <summary>
            /// Logs an event as ended.
            /// </summary>
            /// <param name="eventId">The ID of the event. Should already have started but not ended already.</param>
            /// <param name="instanceKey">The instance key of the event. Must match the one used starting the event.</param>
            /// <param name="result">The result of the event.</param>
            /// <returns>Instance key.</returns>
            internal virtual void EndEvent(int instanceKey, ResultType result)
            {
                if (!this._instanceKeyMap.ContainsKey(instanceKey))
                {
                    LogWarning($"Attempted to end an event that's not started or invalid. Instance ID: {instanceKey}");
                    return;
                }

                var eventId = _instanceKeyMap[instanceKey];
                AnnotateEvent(instanceKey, AnnotationKey.EndTimeStamp,
                    ElapsedMilliseconds.ToString());
                QplMarkerEnd((int)eventId, result, instanceKey, -1);
                _instanceKeyMap.Remove(instanceKey);
                LogVerbose($"Ended telemetry event {eventId}:{instanceKey}({result})");
            }

            public void ShutdownTelemetry()
            {
                try
                {
                    foreach (var instanceKey in _instanceKeyMap.Keys)
                    {
                        AnnotateEvent(instanceKey, AnnotationKey.Error, "Telemetry event not ended gracefully");
                        EndEvent(instanceKey, ResultType.Cancel);
                    }
                }
                finally
                {
                    OnEditorShutdown();
                }
            }

            private static long ElapsedMilliseconds
            {
                get => DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            }

            #region Telemetry native methods
#if UNITY_EDITOR_WIN
            [DllImport(PluginName)]
            private static extern bool QplMarkerStart(int markerId, int instanceKey, long timestampMs);

            [DllImport(PluginName)]
            private static extern bool QplMarkerEnd(int markerId, ResultType boolTypeId,
                int instanceKey, long timestampMs);

            [DllImport(PluginName)]
            private static extern bool QplMarkerPointCached(int markerId, int nameHandle, int instanceKey,
                long timestampMs);

            [DllImport(PluginName)]
            private static extern bool QplMarkerAnnotation(int markerId,
                [MarshalAs(UnmanagedType.LPStr)] string annotationKey,
                [MarshalAs(UnmanagedType.LPStr)] string annotationValue, int instanceKey);

            [DllImport(PluginName)]
            private static extern bool QplCreateMarkerHandle([MarshalAs(UnmanagedType.LPStr)] string name,
                out int nameHandle);

            [DllImport(PluginName)]
            private static extern bool QplDestroyMarkerHandle(int nameHandle);

            [DllImport(PluginName, CallingConvention = CallingConvention.Cdecl)]
            private static extern bool OnEditorShutdown();
#else
            private static bool QplMarkerStart(int markerId, int instanceKey, long timestampMs)
            {
                return false;
            }

            private static bool QplMarkerEnd(int markerId, ResultType boolTypeId,
                int instanceKey, long timestampMs)
            {
                return false;
            }

            private static bool QplMarkerPointCached(int markerId, int nameHandle, int instanceKey,
                long timestampMs)
            {
                return false;
            }

            private static bool QplMarkerAnnotation(int markerId,
                [MarshalAs(UnmanagedType.LPStr)] string annotationKey,
                [MarshalAs(UnmanagedType.LPStr)] string annotationValue, int instanceKey)
            {
                return false;
            }

            private static bool QplCreateMarkerHandle([MarshalAs(UnmanagedType.LPStr)] string name,
                out int nameHandle)
            {
                nameHandle = -1;
                return false;
            }

            private static bool QplDestroyMarkerHandle(int nameHandle)
            {
                return false;
            }

            private static bool OnEditorShutdown()
            {
                return false;
            }
#endif
            #endregion
        }

        /// <summary>
        /// This instance will be used when we don't have consent to collect telemetry.
        /// </summary>
        private class LocalTelemetry : TelemetryChannel
        {
            private bool _warnOnConsent = true;

            /// <inheritdoc/>
            internal override int StartEvent(TelemetryEventId eventId)
            {
                if (_warnOnConsent)
                {
                    LogWarning("Not sending telemetry. User consent required.");
                    _warnOnConsent = false;
                }
                return 0;
            }

            /// <inheritdoc/>
            internal override void LogInstantEvent(TelemetryEventId eventId, Dictionary<AnnotationKey, string> annotations = null)
            {
            }

            /// <inheritdoc/>
            internal override void AnnotateEvent(int instanceKey, AnnotationKey key, string value)
            {
            }

            /// <inheritdoc/>
            internal override void EndEvent(int instanceKey, ResultType result)
            {
            }
        }

        /// <summary>
        /// The result of an event.
        /// </summary>
        public enum ResultType : short
        {
            Success = 2,
            Failure = 3,
            Cancel = 4
        }

        /// <summary>
        /// The event IDs. These should map to GQL IDs.
        /// </summary>
        public enum TelemetryEventId
        {
            Unknown = 92612351,
            Session = 92611421,
            SupplyToken = 92608491,
            CheckAutoTrain = 92612591,
            AutoTrain = 92617773,
            ToggleCheckbox = 1063854409,
            SyncEntities = 92609990,
            ClickButton = 92615194,
            AssignIntentMatcherInInspector = 92616101,
            SelectOption = 92604542,
            GenerateManifest = 92615944,
            LoadManifest = 92613324,
            NavigateToCodeFromInspector = 92614941,
            OpenUi = 92610372,
        }

        /// <summary>
        /// The annotation keys used for the key-value annotations.
        /// </summary>
        public enum AnnotationKey
        {
            Unknown,
            UnrecognizedEvent,
            UnrecognizedAnnotationKey,
            EnvironmentId,
            SessionId,
            StartTimeStamp,
            EndTimeStamp,
            Error,
            PageId,
            CompatibleSignatures,
            IncompatibleSignatures,
            IsAvailable,
            ControlId,
            Value,
            Type,
        }
    }
}
