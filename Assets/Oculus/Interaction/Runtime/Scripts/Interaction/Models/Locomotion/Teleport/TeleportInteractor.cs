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
using System;
using UnityEngine;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// TeleportInteractor uses a provided ITeleportArc and an external Selector to find
    /// the best interactable and emit the event requesting a teleport to it. By itself
    /// it does not define the shape of the arc or even moves the player, instead it is the
    /// core class that brings these pieces together.
    /// </summary>
    public class TeleportInteractor : Interactor<TeleportInteractor, TeleportInteractable>,
        ILocomotionEventBroadcaster
    {
        [SerializeField, Interface(typeof(ISelector))]
        [Tooltip("A selector indicating when the Interactor should" +
            "Select or Unselect the best available interactable." +
            "Typically when using controllers this selector is drivenby the joystick value," +
            "and for hands it is driven by the index pinch value.")]
        private MonoBehaviour _selector;

        [SerializeField, Optional, Interface(typeof(IHmd))]
        [Tooltip("When provided, the Interactor will perform an extra check to ensure" +
            "nothing is blocking the line between the Hmd and the teleport origin")]
        private MonoBehaviour _hmd;
        /// <summary>
        /// When provided, the Interactor will perform an extra check to ensure nothing
        /// is blocking the line between the Hmd (head of the player) and the teleport
        /// origin (hand). Making it impossible to teleport if the user is placing their
        /// hands behind a virtual wall.
        /// </summary>
        private IHmd Hmd { get; set; }

        [SerializeField]
        [Tooltip("Transform indicating the position and direction (forward) from where the arc starts.")]
        private Transform _arcOrigin;

        [SerializeField, Optional, Interface(typeof(ITeleportArc))]
        [Tooltip("Specifies the shape of the arc used for detecting available interactables." +
            "If none is provided TeleportArcGravity will be used.")]
        private MonoBehaviour _teleportArc;
        /// <summary>
        ///  Specifies the shape of the arc used for detecting available interactables.
        ///  If none is provided TeleportArcGravity will be used.
        /// </summary>
        public ITeleportArc TeleportArc { get; private set; }

        [SerializeField]
        [Tooltip("(Meters, World) The threshold below which distances to a interactable " +
                 "are treated as equal for the purposes of ranking.")]
        private float _equalDistanceThreshold = 0.1f;
        /// <summary>
        /// (Meters, World) The threshold below which distances to a interactable are treated as equal for the purposes of ranking.
        /// </summary>
        public float EqualDistanceThreshold
        {
            get
            {
                return _equalDistanceThreshold;
            }
            set
            {
                _equalDistanceThreshold = value;
            }
        }

        public Pose ArcOrigin => _arcOrigin.GetPose();

        private TeleportHit _arcEnd;
        public TeleportHit ArcEnd => _arcEnd;

        public Pose TeleportTarget
        {
            get
            {
                Vector3 forward = Vector3.ProjectOnPlane(ArcOrigin.forward, _arcEnd.Normal);
                Quaternion rotation = Quaternion.LookRotation(forward, _arcEnd.Normal);
                Pose pose = new Pose(_arcEnd.Point, rotation);
                if (HasInteractable)
                {
                    return Interactable.TargetPose(pose);
                }
                return pose;
            }
        }

        private Action<LocomotionEvent> _whenLocomotionPerformed = delegate { };
        public event Action<LocomotionEvent> WhenLocomotionPerformed
        {
            add
            {
                _whenLocomotionPerformed += value;
            }
            remove
            {
                _whenLocomotionPerformed -= value;
            }
        }

        private Action _whenLocomotionDenied = delegate { };
        public event Action WhenLocomotionDenied
        {
            add
            {
                _whenLocomotionDenied += value;
            }
            remove
            {
                _whenLocomotionDenied -= value;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            TeleportArc = _teleportArc as ITeleportArc;
            Selector = _selector as ISelector;
            Hmd = _hmd as IHmd;
        }

        protected override void Start()
        {
            base.Start();
            this.AssertField(Selector, nameof(Selector));
            this.AssertField(_arcOrigin, nameof(_arcOrigin));

            if (TeleportArc == null)
            {
                var arc = this.gameObject.AddComponent<TeleportArcGravity>();
                InjectOptionalTeleportArc(arc);
            }
        }

        public override bool CanSelect(TeleportInteractable interactable)
        {
            Pose origin = ArcOrigin;
            float maxDistance = TeleportArc.MaxDistance;
            float maxSqrDistance = maxDistance * maxDistance;

            if (!interactable.IsInRange(origin, maxSqrDistance))
            {
                return false;
            }

            return base.CanSelect(interactable);
        }

        protected override void InteractableSelected(TeleportInteractable interactable)
        {
            base.InteractableSelected(interactable);
            if (interactable == null
                || !interactable.AllowTeleport)
            {
                _whenLocomotionDenied.Invoke();
                return;
            }

            LocomotionEvent locomotionEvent = new LocomotionEvent(this.Identifier, TeleportTarget,
                interactable.EyeLevel ?
                LocomotionEvent.TranslationType.AbsoluteEyeLevel
                : LocomotionEvent.TranslationType.Absolute,
                interactable.FaceTargetDirection ?
                LocomotionEvent.RotationType.Absolute
                : LocomotionEvent.RotationType.None);
            _whenLocomotionPerformed.Invoke(locomotionEvent);
        }

        protected override TeleportInteractable ComputeCandidate()
        {
            Pose origin = _arcOrigin.GetPose();
            float bestScore = float.PositiveInfinity;
            Vector3 arcEndPosition = TeleportArc.PointAtIndex(origin, TeleportArc.ArcPointsCount - 1);

            TeleportInteractable bestCandidate = null;
            TeleportHit bestHit = new TeleportHit(null, arcEndPosition, Vector3.up);
            var interactables = TeleportInteractable.Registry.List(this);

            Pose headPose = Pose.identity;
            if (Hmd != null)
            {
                Hmd.TryGetRootPose(out headPose);
            }

            foreach (TeleportInteractable interactable in interactables)
            {
                if (interactable.AllowTeleport)
                {
                    continue;
                }

                if (Hmd != null)
                {
                    CheckViewToOriginBlockers(headPose.position, interactable);
                }
                CheckCandidate(interactable);
            }

            foreach (TeleportInteractable interactable in interactables)
            {
                if (interactable.AllowTeleport)
                {
                    CheckCandidate(interactable);
                }
            }

            _arcEnd = bestHit;
            return bestCandidate;

            void CheckViewToOriginBlockers(Vector3 viewPosition, TeleportInteractable candidate)
            {
                if (candidate.DetectHit(viewPosition, _arcOrigin.position, out TeleportHit hit))
                {
                    float score = -Vector3.Distance(_arcOrigin.position, hit.Point);
                    bool isTie = Mathf.Abs(bestScore - score) < _equalDistanceThreshold;
                    if (bestCandidate == null
                            || (!isTie && score < bestScore)
                            || (isTie && candidate.TieBreakerScore > bestCandidate.TieBreakerScore))
                    {
                        bestScore = score;
                        bestHit = hit;
                        bestCandidate = candidate;
                    }
                }
            }

            void CheckCandidate(TeleportInteractable candidate)
            {
                Vector3 prevPoint = origin.position;
                float accumulatedDistance = 0;
                for (int i = 1; i < TeleportArc.ArcPointsCount; i++)
                {
                    if (accumulatedDistance > bestScore)
                    {
                        break;
                    }
                    Vector3 point = TeleportArc.PointAtIndex(origin, i);
                    if (candidate.DetectHit(prevPoint, point, out TeleportHit hit))
                    {
                        float score = accumulatedDistance
                            + Vector3.Distance(prevPoint, hit.Point);

                        bool isTie = Mathf.Abs(bestScore - score) < _equalDistanceThreshold;
                        if (!isTie
                            && bestCandidate != null
                            && !bestCandidate.AllowTeleport
                            && candidate.AllowTeleport)
                        {
                            float snapRadius = hit.SnapRadius;
                            float pointToPoint = (bestHit.Point - hit.Point).sqrMagnitude;
                            if (pointToPoint < snapRadius * snapRadius)
                            {
                                isTie = true;
                            }
                        }

                        if (bestCandidate == null
                            || (!isTie && score < bestScore)
                            || (isTie && candidate.TieBreakerScore > bestCandidate.TieBreakerScore))
                        {
                            bestScore = score;
                            bestHit = hit;
                            bestCandidate = candidate;
                            break;
                        }
                    }
                    accumulatedDistance += Vector3.Distance(prevPoint, point);
                    prevPoint = point;
                }
            }
        }

        #region Inject
        public void InjectAllTeleportInteractor(ISelector selector,
            Transform arcOrigin)
        {
            InjectSelector(selector);
            InjectArcOrigin(arcOrigin);
        }
        public void InjectSelector(ISelector selector)
        {
            _selector = selector as MonoBehaviour;
            Selector = selector;
        }
        public void InjectArcOrigin(Transform arcOrigin)
        {
            _arcOrigin = arcOrigin;
        }
        public void InjectOptionalHmd(IHmd hmd)
        {
            _hmd = hmd as MonoBehaviour;
            Hmd = hmd;
        }
        public void InjectOptionalTeleportArc(ITeleportArc teleportArc)
        {
            _teleportArc = teleportArc as MonoBehaviour;
            TeleportArc = teleportArc;
        }
        #endregion
    }
}
