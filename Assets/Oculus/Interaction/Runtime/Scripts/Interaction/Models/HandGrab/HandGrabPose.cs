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

using Oculus.Interaction.Grab;
using Oculus.Interaction.Grab.GrabSurfaces;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// The HandGrabPose defines the local point in an object to which the grip point
    /// of the hand should align. It can also contain information about the final pose
    /// of the hand for perfect alignment as well as a surface that indicates the valid
    /// positions for the point.
    /// </summary>
    public class HandGrabPose : MonoBehaviour,
        IRelativeToRef
    {
        [SerializeField]
        private Transform _relativeTo;

        [SerializeField, Optional, Interface(typeof(IGrabSurface))]
        private MonoBehaviour _surface = null;
        private IGrabSurface _snapSurface;
        public IGrabSurface SnapSurface
        {
            get => _snapSurface ?? _surface as IGrabSurface;
            private set
            {
                _snapSurface = value;
            }
        }

        [SerializeField]
        private bool _usesHandPose = true;

        [SerializeField, Optional]
        [HideInInspector]
        private HandPose _handPose = new HandPose();

        public HandPose HandPose => _usesHandPose ? _handPose : null;
        public float Scale => this.transform.lossyScale.x;
        public Transform RelativeTo { get => _relativeTo; set => _relativeTo = value; }
        public Pose RelativeGrip => RelativeTo.Delta(this.transform);

        #region editor events

        protected virtual void Reset()
        {
            _relativeTo = this.GetComponentInParent<HandGrabInteractable>()?.RelativeTo;
        }
        #endregion

        protected virtual void Start()
        {
            this.AssertField(_relativeTo, nameof(_relativeTo));
        }

        public bool UsesHandPose()
        {
            return _usesHandPose;
        }

        public bool SupportsHandedness(Handedness handedness)
        {
            if (!_usesHandPose)
            {
                return true;
            }

            return HandPose.Handedness == handedness;
        }

        /// <summary>
        /// Applies the given position/rotation to the HandGrabPose
        /// </summary>
        /// <param name="handPose">Relative hand position/rotation.</param>
        /// <param name="relativeTo">Reference coordinates for the pose.</param>
        public void SetPose(HandPose handPose, in Pose gripPoint, Transform relativeTo)
        {
            _handPose = new HandPose(handPose);
            _relativeTo = relativeTo;
            this.transform.SetPose(relativeTo.GlobalPose(gripPoint));
        }

        public virtual bool CalculateBestPose(Pose userPose, float handScale,
            Handedness handedness, PoseMeasureParameters scoringModifier,
            ref HandGrabResult result)
        {
            result.HasHandPose = false;
            if (HandPose != null && HandPose.Handedness != handedness)
            {
                return false;
            }

            result.Score = CompareNearPoses(userPose, scoringModifier, ref result.SnapPose);
            if (HandPose != null)
            {
                result.HasHandPose = true;
                result.HandPose.CopyFrom(HandPose);
            }

            return true;
        }

        /// <summary>
        /// Finds the most similar pose at this HandGrabInteractable to the user hand pose
        /// </summary>
        /// <param name="worldPoint">The user current hand pose.</param>
        /// <param name="bestSnapPoint">The snap point hand pose within the surface (if any).</param>
        /// <returns>The adjusted best pose at the surface.</returns>
        private GrabPoseScore CompareNearPoses(in Pose worldPoint, PoseMeasureParameters scoringModifier, ref Pose bestSnapPoint)
        {
            Pose desired = worldPoint;
            Pose snap = this.transform.GetPose();

            GrabPoseScore bestScore;
            Pose bestPlace;
            if (SnapSurface != null)
            {
                bestScore = SnapSurface.CalculateBestPoseAtSurface(desired, snap, out bestPlace, scoringModifier);
            }
            else
            {
                bestPlace = snap;
                bestScore = new GrabPoseScore(desired, snap, scoringModifier.PositionRotationWeight);
            }

            _relativeTo.Delta(bestPlace, ref bestSnapPoint);

            return bestScore;
        }

        #region Inject

        public void InjectRelativeTo(Transform relativeTo)
        {
            _relativeTo = relativeTo;
        }

        public void InjectOptionalSurface(IGrabSurface surface)
        {
            _surface = surface as MonoBehaviour;
            SnapSurface = surface;
        }

        public void InjectOptionalHandPose(HandPose handPose)
        {
            _handPose = handPose;
            _usesHandPose = _handPose != null;
        }

        public void InjectAllHandGrabPose(Transform relativeTo)
        {
            InjectRelativeTo(relativeTo);
        }
        #endregion

    }
}
