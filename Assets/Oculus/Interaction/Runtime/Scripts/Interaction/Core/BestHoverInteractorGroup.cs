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

using System.Collections.Generic;

namespace Oculus.Interaction
{
    /// <summary>
    /// This interactor group allows only the highest-priority interactor
    /// to be the one in Hover, and the rest will be disabled until it
    /// unhovers or it is superseed but a higher-priority interactor.
    /// </summary>
    public class BestHoverInteractorGroup : InteractorGroup
    {
        private IInteractor _bestInteractor = null;
        private int _bestInteractorIndex = -1;

        private static readonly InteractorPredicate IsNormalAndShouldHoverPredicate =
            (interactor, index) => interactor.State == InteractorState.Normal && interactor.ShouldHover;

        public override bool ShouldHover
        {
            get
            {
                if (State != InteractorState.Normal)
                {
                    return false;
                }

                return AnyInteractor(IsNormalAndShouldHoverPredicate);

            }
        }

        public override bool ShouldUnhover
        {
            get
            {
                if (State != InteractorState.Hover)
                {
                    return false;
                }

                return _bestInteractor != null
                        && _bestInteractor.ShouldUnhover;
            }
        }

        public override bool ShouldSelect
        {
            get
            {
                if (State != InteractorState.Hover)
                {
                    return false;
                }

                return _bestInteractor != null
                    && _bestInteractor.ShouldSelect;
            }
        }

        public override bool ShouldUnselect
        {
            get
            {
                if (State != InteractorState.Select)
                {
                    return false;
                }

                return _bestInteractor != null
                        && _bestInteractor.ShouldUnselect;
            }
        }

        public override void Hover()
        {
            if (TryHover())
            {
                State = InteractorState.Hover;
            }
        }

        private bool TryHover(int betterThan = -1, int skipIndex = -1)
        {
            if (TryGetBestCandidateIndex(IsNormalAndShouldHoverPredicate,
                out int interactorIndex, betterThan, skipIndex))
            {
                HoverAtIndex(interactorIndex);
                return true;
            }
            return false;
        }

        private bool TryReplaceHover(int betterThan)
        {
            for (int i = 0; i < Interactors.Count; i++)
            {
                IInteractor interactor = Interactors[i];
                if (interactor.State != InteractorState.Disabled)
                {
                    continue;
                }
                interactor.Enable();
                if (interactor.State == InteractorState.Normal)
                {
                    interactor.ProcessCandidate();
                }
            }

            if (TryHover(betterThan, _bestInteractorIndex))
            {
                return true;
            }

            DisableAllExcept(_bestInteractor);
            return false;
        }

        private void HoverAtIndex(int interactorIndex)
        {
            UnsuscribeBestInteractor();
            _bestInteractorIndex = interactorIndex;
            _bestInteractor = Interactors[_bestInteractorIndex];
            _bestInteractor.Hover();
            _bestInteractor.WhenStateChanged += HandleBestInteractorStateChanged;
            DisableAllExcept(_bestInteractor);
        }

        public override void Unhover()
        {
            if (State != InteractorState.Hover)
            {
                return;
            }

            if (_bestInteractor != null)
            {
                _bestInteractor.Unhover();
                if (_bestInteractor != null
                    && _bestInteractor.State == InteractorState.Hover)
                {
                    return;
                }
            }
            State = InteractorState.Normal;
        }

        public override void Select()
        {
            if (State != InteractorState.Hover)
            {
                return;
            }

            _bestInteractor.Select();

            State = InteractorState.Select;
        }

        public override void Unselect()
        {
            if (State != InteractorState.Select)
            {
                return;
            }

            if (_bestInteractor != null)
            {
                _bestInteractor.Unselect();
                if (_bestInteractor != null
                    && _bestInteractor.State == InteractorState.Select)
                {
                    return;
                }
            }

            State = InteractorState.Hover;
        }

        public override void Preprocess()
        {
            base.Preprocess();

            if (_bestInteractor == null
                && (State == InteractorState.Hover || State == InteractorState.Select))
            {
                this.ProcessCandidate();
                base.Process();
                if (TryHover())
                {
                    if (State == InteractorState.Select)
                    {
                        _bestInteractor.Process();
                        if (ShouldSelect)
                        {
                            Select();
                            State = InteractorState.Select;
                            return;
                        }
                    }

                    State = InteractorState.Hover;
                    return;
                }

                if (State == InteractorState.Select)
                {
                    State = InteractorState.Hover;
                }
                if (State == InteractorState.Hover)
                {
                    State = InteractorState.Normal;
                }
            }
            else if (_bestInteractor != null
                && State == InteractorState.Select
                && _bestInteractor.State == InteractorState.Hover)
            {
                State = InteractorState.Hover;
            }
        }

        public override void Process()
        {
            base.Process();

            if (_bestInteractor != null
               && State == InteractorState.Hover)
            {
                if (TryReplaceHover(_bestInteractorIndex))
                {
                    _bestInteractor.Process();
                }
            }
        }

        private void HandleBestInteractorStateChanged(InteractorStateChangeArgs stateChange)
        {
            if (stateChange.PreviousState == InteractorState.Hover
                && stateChange.NewState == InteractorState.Normal)
            {
                IInteractor prevBest = _bestInteractor;
                UnsuscribeBestInteractor();
                EnableAllExcept(prevBest);
            }
        }

        public override void Enable()
        {
            if (_bestInteractor != null)
            {
                _bestInteractor.Enable();
            }
            else
            {
                base.Enable();
            }
        }

        public override void Disable()
        {
            UnsuscribeBestInteractor();
            base.Disable();
        }

        private void UnsuscribeBestInteractor()
        {
            if (_bestInteractor != null)
            {
                _bestInteractor.WhenStateChanged -= HandleBestInteractorStateChanged;
                _bestInteractor = null;
                _bestInteractorIndex = -1;
            }
        }

        public override bool HasCandidate
        {
            get
            {
                if (_bestInteractor != null && _bestInteractor.HasCandidate)
                {
                    return true;
                }
                return AnyInteractor(HasCandidatePredicate);
            }
        }
        public override bool HasInteractable
        {
            get
            {
                return _bestInteractor != null && _bestInteractor.HasInteractable;
            }
        }
        public override bool HasSelectedInteractable
        {
            get
            {
                return _bestInteractor != null && _bestInteractor.HasSelectedInteractable;
            }
        }
        public override object CandidateProperties
        {
            get
            {
                if (_bestInteractor != null && _bestInteractor.HasCandidate)
                {
                    return _bestInteractor.CandidateProperties;
                }
                if (TryGetBestCandidateIndex(TruePredicate, out int interactorIndex))
                {
                    return Interactors[interactorIndex].CandidateProperties;
                }
                else
                {
                    return null;
                }
            }
        }

        #region Inject
        public void InjectAllInteractorGroupBestHover(List<IInteractor> interactors)
        {
            base.InjectAllInteractorGroupBase(interactors);
        }
        #endregion
    }
}
