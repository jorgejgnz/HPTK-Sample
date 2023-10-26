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
    /// This interactor group allows many Interactors to be in Hover state
    /// but upon selection, the one with the highest priority will be the
    /// one Selecting and the rest will be disabled until it unselects.
    /// </summary>
    public class BestSelectInteractorGroup : InteractorGroup
    {
        private IInteractor _bestInteractor = null;

        private static readonly InteractorPredicate IsNormalAndShouldHoverPredicate =
            (interactor, index) => interactor.State == InteractorState.Normal && interactor.ShouldHover;

        private static readonly InteractorPredicate IsHoverAndShouldUnhoverPredicate =
            (interactor, index) => interactor.State == InteractorState.Hover && interactor.ShouldUnhover;

        private static readonly InteractorPredicate IsHoverAndShoulSelectPredicate =
            (interactor, index) => interactor.State == InteractorState.Hover && interactor.ShouldSelect;

        private static readonly InteractorPredicate IsHover =
            (interactor, index) => interactor.State == InteractorState.Hover;

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

                return AnyInteractor(IsHoverAndShouldUnhoverPredicate)
                    || !AnyInteractor(IsHover);
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

                return AnyInteractor(IsHoverAndShoulSelectPredicate);
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

        private bool TryHover(System.Action<IInteractor> whenHover = null)
        {
            bool anyHover = false;

            while (TryGetBestCandidateIndex(IsNormalAndShouldHoverPredicate, out int index))
            {
                IInteractor interactor = Interactors[index];
                interactor.Hover();
                whenHover?.Invoke(Interactors[index]);
                anyHover = true;
            }

            return anyHover;
        }

        public override void Unhover()
        {
            if (State != InteractorState.Hover)
            {
                return;
            }

            while (TryGetBestCandidateIndex(IsHoverAndShouldUnhoverPredicate, out int index))
            {
                Interactors[index].Unhover();
            }

            if (!AnyInteractor(IsHover))
            {
                State = InteractorState.Normal;
            }
        }

        public override void Select()
        {
            if (TryGetBestCandidateIndex(IsHoverAndShoulSelectPredicate,
                out int interactorIndex))
            {
                _bestInteractor = Interactors[interactorIndex];
                _bestInteractor.Select();
                _bestInteractor.WhenStateChanged += HandleBestInteractorStateChanged;
                DisableAllExcept(_bestInteractor);
            }

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

            if (_bestInteractor == null && State == InteractorState.Select)
            {
                this.ProcessCandidate();
                base.Process();

                if (TryHover((interactor) => interactor.Process()))
                {
                    if (ShouldSelect)
                    {
                        Select();
                        State = InteractorState.Select;
                        return;
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

            if (State == InteractorState.Hover
                && AnyInteractor(IsNormalAndShouldHoverPredicate))
            {
                if (TryHover((interactor) => interactor.Process()))
                {
                    State = InteractorState.Hover;
                }
            }

            if (State == InteractorState.Hover
               && AnyInteractor(IsHoverAndShouldUnhoverPredicate))
            {
                while (TryGetBestCandidateIndex(IsHoverAndShouldUnhoverPredicate, out int index))
                {
                    IInteractor interactor = Interactors[index];
                    interactor.Unhover();
                    if (interactor.State != InteractorState.Hover)
                    {
                        interactor.Process();
                    }
                }
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
            }
        }

        private void HandleBestInteractorStateChanged(InteractorStateChangeArgs stateChange)
        {
            if (stateChange.PreviousState == InteractorState.Select
                && stateChange.NewState == InteractorState.Hover)
            {
                IInteractor prevBestInteractor = _bestInteractor;
                UnsuscribeBestInteractor();
                EnableAllExcept(prevBestInteractor);
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
                if (_bestInteractor != null)
                {
                    return _bestInteractor.HasInteractable;
                }
                return AnyInteractor(HasInteractablePredicate);
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
        public void InjectAllInteractorGroupBestSelect(List<IInteractor> interactors)
        {
            base.InjectAllInteractorGroupBase(interactors);
        }
        #endregion
    }
}
