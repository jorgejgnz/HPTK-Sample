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

using Oculus.Interaction.Input;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction.Samples.PalmMenu
{
    /// <summary>
    /// Filters to one set of GameObjects or the other, depending on which hand is the user's dominant hand.
    /// </summary>
    public class DominantHandGameObjectFilter : MonoBehaviour, IGameObjectFilter
    {
        [SerializeField, Interface(typeof(IHand))]
        private Object _leftHand;

        [SerializeField]
        private GameObject[] _leftHandedGameObjects;

        [SerializeField]
        private GameObject[] _rightHandedGameObjects;

        private IHand LeftHand { get; set; }

        private readonly HashSet<GameObject> _leftHandedGameObjectSet =
            new HashSet<GameObject>();
        private readonly HashSet<GameObject> _rightHandedGameObjectSet =
            new HashSet<GameObject>();

        protected virtual void Start()
        {
            foreach (var go in _leftHandedGameObjects)
            {
                _leftHandedGameObjectSet.Add(go);
            }

            foreach (var go in _rightHandedGameObjects)
            {
                _rightHandedGameObjectSet.Add(go);
            }

            LeftHand = _leftHand as IHand;
        }

        public bool Filter(GameObject go)
        {
            if (LeftHand.IsDominantHand)
            {
                return _leftHandedGameObjectSet.Contains(go);
            }
            else
            {
                return _rightHandedGameObjectSet.Contains(go);
            }
        }
    }
}
