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

using TMPro;
using UnityEngine;

namespace Oculus.Interaction.Samples.PalmMenu
{
    /// <summary>
    /// Example of a bespoke behavior created to react to a particular palm menu. This controls the state
    /// of the object that responds to the menu, but also parts of the menu itself, specifically those
    /// which depend on the state of the controlled object (swappable icons, various text boxes, etc.).
    /// </summary>
    public class PalmMenuExampleButtonHandlers : MonoBehaviour
    {
        [SerializeField]
        private GameObject _controlledObject;

        [SerializeField]
        private Color[] _colors;

        [SerializeField]
        private GameObject _rotationEnabledIcon;

        [SerializeField]
        private GameObject _rotationDisabledIcon;

        [SerializeField]
        private float _rotationLerpSpeed = 1f;

        [SerializeField]
        private TMP_Text _rotationDirectionText;

        [SerializeField]
        private string[] _rotationDirectionNames;

        [SerializeField]
        private GameObject[] _rotationDirectionIcons;

        [SerializeField]
        private Quaternion[] _rotationDirections;

        [SerializeField]
        private TMP_Text _elevationText;

        [SerializeField]
        private float _elevationChangeIncrement;

        [SerializeField]
        private float _elevationChangeLerpSpeed = 1f;

        [SerializeField]
        private TMP_Text _shapeNameText;

        [SerializeField]
        private string[] _shapeNames;

        [SerializeField]
        private Mesh[] _shapes;

        private int _currentColorIdx;
        private bool _rotationEnabled;
        private int _currentRotationDirectionIdx;
        private Vector3 _targetPosition;
        private int _currentShapeIdx;

        private void Start()
        {
            _currentColorIdx = _colors.Length;
            CycleColor();

            _rotationEnabled = false;
            ToggleRotationEnabled();

            _currentRotationDirectionIdx = _rotationDirections.Length;
            CycleRotationDirection();

            _targetPosition = _controlledObject.transform.position;
            IncrementElevation(true);
            IncrementElevation(false);

            _currentShapeIdx = _shapes.Length;
            CycleShape(true);
        }

        private void Update()
        {
            if (_rotationEnabled)
            {
                var rotation = Quaternion.Slerp(Quaternion.identity, _rotationDirections[_currentRotationDirectionIdx], _rotationLerpSpeed * Time.deltaTime);
                _controlledObject.transform.rotation = rotation * _controlledObject.transform.rotation;
            }

            _controlledObject.transform.position = Vector3.Lerp(_controlledObject.transform.position, _targetPosition, _elevationChangeLerpSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Change the color of the controlled object to the next in the list of allowed colors, looping if the end of the list is reached.
        /// </summary>
        public void CycleColor()
        {
            _currentColorIdx += 1;
            if (_currentColorIdx >= _colors.Length)
            {
                _currentColorIdx = 0;
            }

            _controlledObject.GetComponent<Renderer>().material.SetColor("_Color", _colors[_currentColorIdx]);
        }

        /// <summary>
        /// Toggle whether or not rotation is enabled, and set the icon of the controlling button to display what will happen next time the button is pressed.
        /// </summary>
        public void ToggleRotationEnabled()
        {
            _rotationEnabled = !_rotationEnabled;
            _rotationEnabledIcon.SetActive(!_rotationEnabled);
            _rotationDisabledIcon.SetActive(_rotationEnabled);
        }

        /// <summary>
        /// Change the rotation direction of the controlled object to the next in the list of allowed directions, looping if the end of the list is reached.
        /// Set the icon of the controlling button to display what will happen next time the button is pressed.
        /// </summary>
        public void CycleRotationDirection()
        {
            Debug.Assert(_rotationDirectionNames.Length == _rotationDirections.Length);
            Debug.Assert(_rotationDirectionNames.Length == _rotationDirectionIcons.Length);

            _currentRotationDirectionIdx += 1;
            if (_currentRotationDirectionIdx >= _rotationDirections.Length)
            {
                _currentRotationDirectionIdx = 0;
            }

            int nextRotationDirectionIdx = _currentRotationDirectionIdx + 1;
            if (nextRotationDirectionIdx >= _rotationDirections.Length)
            {
                nextRotationDirectionIdx = 0;
            }

            _rotationDirectionText.text = _rotationDirectionNames[nextRotationDirectionIdx];
            for (int idx = 0; idx < _rotationDirections.Length; ++idx)
            {
                _rotationDirectionIcons[idx].SetActive(idx == nextRotationDirectionIdx);
            }
        }

        /// <summary>
        /// Change the target elevation of the controlled object in the requested direction, within the limits [0.2, 2].
        /// Set the text to display the new target elevation.
        /// </summary>
        public void IncrementElevation(bool up)
        {
            float increment = _elevationChangeIncrement;
            if (!up)
            {
                increment *= -1f;
            }
            _targetPosition = new Vector3(_targetPosition.x, Mathf.Clamp(_targetPosition.y + increment, 0.2f, 2f), _targetPosition.z);
            _elevationText.text = "Elevation: " + _targetPosition.y.ToString("0.0");
        }

        /// <summary>
        /// Change the shape of the controlled object to the next or previous in the list of allowed shapes, depending on the requested direction, looping beyond the bounds of the list.
        /// Set the text to display the name of the current shape.
        /// </summary>
        public void CycleShape(bool cycleForward)
        {
            Debug.Assert(_shapeNames.Length == _shapes.Length);

            _currentShapeIdx += cycleForward ? 1 : -1;
            if (_currentShapeIdx >= _shapes.Length)
            {
                _currentShapeIdx = 0;
            }
            else if (_currentShapeIdx < 0)
            {
                _currentShapeIdx = _shapes.Length - 1;
            }

            _shapeNameText.text = _shapeNames[_currentShapeIdx];
            _controlledObject.GetComponent<MeshFilter>().mesh = _shapes[_currentShapeIdx];
        }
    }
}
