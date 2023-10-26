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
using UnityEngine;

namespace Oculus.Interaction.Samples.PalmMenu
{
    /// <summary>
    /// Matches the position and rotation of the user's dominant hand in world space. Normally this
    /// sort of behavior is done using the transform hierarchy, but in this case doing it via a script
    /// is cleaner as it (1) allows us to avoid nesting elements under the hand prefab itself and (2)
    /// allows the behavior to easily swap between hands depending on which hand is dominant. This
    /// also provides a convenient location for the rotation math that keeps the menu aligned "y-up"
    /// while still facing the "aim point" and located at the "anchor point." The default anchor- and
    /// aim-point values roughly center the menu just above the palms, facing away from the hands.
    /// </summary>
    public class MatchNonDominantPalmWorldSpaceTransform : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IHand))]
        private Object _leftHand;

        [SerializeField, Interface(typeof(IHand))]
        private Object _rightHand;

        [SerializeField]
        private Vector3 _leftAnchorPoint = new Vector3(-0.0608603321f, 0.00953984447f, 0.000258127693f);

        [SerializeField]
        private Vector3 _leftAimPoint = new Vector3(-0.0749258399f, 0.0893092677f, 0.000258127693f);

        [SerializeField]
        private Vector3 _rightAnchorPoint = new Vector3(0.0652603358f, -0.011439844f, -0.00455812784f);

        [SerializeField]
        private Vector3 _rightAimPoint = new Vector3(0.0793258473f, -0.0912092775f, -0.00455812784f);

        private IHand LeftHand { get; set; }
        private IHand RightHand { get; set; }

        protected virtual void Awake()
        {
            LeftHand = _leftHand as IHand;
            RightHand = _rightHand as IHand;
        }

        private void Update()
        {
            var anchor = LeftHand.IsDominantHand ? _rightAnchorPoint : _leftAnchorPoint;
            var aim = LeftHand.IsDominantHand ? _rightAimPoint : _leftAimPoint;
            var hand = LeftHand.IsDominantHand ? RightHand : LeftHand;
            Pose wristPose;
            if (hand.GetJointPose(HandJointId.HandWristRoot, out wristPose))
            {
                var anchorPose = new Pose(anchor, Quaternion.identity).GetTransformedBy(wristPose);
                var aimPose = new Pose(aim, Quaternion.identity).GetTransformedBy(wristPose);
                this.transform.SetPositionAndRotation(anchorPose.position, Quaternion.LookRotation((aimPose.position - anchorPose.position).normalized));
            }
        }
    }
}
