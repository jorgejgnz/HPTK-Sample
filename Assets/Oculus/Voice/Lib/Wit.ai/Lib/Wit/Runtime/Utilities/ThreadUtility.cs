/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine;
using System.Collections;
using System.Threading;

namespace Meta.WitAi
{
    public static class ThreadUtility
    {
        // Default timeout to off
        public const float THREAD_DEFAULT_TIMEOUT = -1f;

        // Perform in background & return on complete
        public static ThreadPerformer PerformInBackground(Func<bool> workerAction, Action<bool> onComplete, float timeout = THREAD_DEFAULT_TIMEOUT)
        {
            return new ThreadPerformer(workerAction, onComplete, timeout);
        }

        // Performer
        public class ThreadPerformer
        {
            /// <summary>
            /// Whether thread is running
            /// </summary>
            public bool IsRunning { get; private set; }

            // Complete callback items
            private Thread _thread;
            private Func<bool> _worker;
            private Action<bool> _complete;
            private float _timeout;
            private bool _success;
            private CoroutineUtility.CoroutinePerformer _coroutine;

            /// <summary>
            /// Generate thread
            /// </summary>
            public ThreadPerformer(Func<bool> worker, Action<bool> onComplete, float timeout)
            {
                // Begin
                IsRunning = true;

                // Wait for thread completion
                _success = true;
                _worker = worker;
                _complete = onComplete;
                _timeout = timeout;
                _coroutine = CoroutineUtility.StartCoroutine(WaitForCompletion(), true);

                // Start thread
                _thread = new Thread(Work);
                _thread.Start();
            }

            // Work
            private void Work()
            {
                // Perform action
                try
                {
                    _success = _worker.Invoke();
                }
                // Catch exceptions
                catch (Exception e)
                {
                    VLog.E($"Background thread error thrown\n{e}");
                    _success = false;
                }

                // Complete
                IsRunning = false;
            }

            // Wait for completion
            private IEnumerator WaitForCompletion()
            {
                // Wait while running
                DateTime start = DateTime.Now;
                while (IsRunning && !IsTimedOut(start))
                {
                    yield return null;
                }

                // Timed out
                if (IsTimedOut(start))
                {
                    _success = false;
                }

                // Complete
                _complete?.Invoke(_success);

                // Quit
                Quit();
            }
            // Check if timed out
            private bool IsTimedOut(DateTime start)
            {
                // Ignore if no timeout
                if (_timeout <= 0)
                {
                    return false;
                }
                // Timed out
                return (DateTime.Now - start).TotalSeconds >= _timeout;
            }

            // Quit running thread
            public void Quit()
            {
                if (_coroutine != null)
                {
                    GameObject.DestroyImmediate(_coroutine);
                    _coroutine = null;
                }
                if (IsRunning)
                {
                    IsRunning = false;
                    _thread.Join();
                }
                _thread = null;
            }
        }
    }
}

