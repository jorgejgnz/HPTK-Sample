/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using Meta.WitAi;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Meta.Voice.Samples.TTSVoices
{
    public class SimpleDropdownIndexEvent : UnityEvent<int> {}
    public class SimpleDropdownOptionEvent : UnityEvent<string> {}

    /// <summary>
    /// A dropdown ui interface that uses 'UnityEngine.Text' labels and
    /// builds content via a load method that accepts a list of strings.
    /// </summary>
    public class SimpleDropdownList : MonoBehaviour
    {
        [Header("Dropdown Toggle UI")]
        [SerializeField] [Tooltip("Button used for speaker dropdown toggle")]
        private Toggle _dropdownToggle;
        [SerializeField] [Tooltip("Button arrow image showing open or closed")]
        private Transform _dropdownButtonArrowImage;
        [SerializeField] [Tooltip("Button text to be displayed without option selected")]
        public string DropdownToggleUnselectedText = "...";

        [Header("Dropdown List UI")]
        [SerializeField] [Tooltip("Canvas used for dropdown popup")]
        private CanvasGroup _dropdownListPopup;
        [SerializeField] [Tooltip("Dropdown scroll rect used for the dropdown list")]
        private ScrollRect _dropdownListScrollRect;
        [SerializeField] [Tooltip("Prefab used for canvas dropdown list")]
        private Toggle _dropdownListCellPrefab;
        [SerializeField] [Tooltip("Additional text padding for dropdown cells")]
        private float _dropdownCellTextPadding = 2f;

        [SerializeField] [Tooltip("All available options for this dropdown")]
        private string[] _options;
        public string[] Options => _options;

        [Header("Dropdown Default Settings")]
        [SerializeField] [Tooltip("The current option selected")]
        private int _selectedOptionIndex = -1;
        public int SelectedOptionIndex => _selectedOptionIndex;

        // Accessor for selected option
        public string SelectedOption
        {
            get
            {
                if (_options != null && SelectedOptionIndex >= 0 && SelectedOptionIndex < _options.Length)
                {
                    return _options[SelectedOptionIndex];
                }
                return null;
            }
        }

        [SerializeField] [Tooltip("Dropdown callback event for selection by int")]
        private SimpleDropdownIndexEvent _onIndexSelected = new SimpleDropdownIndexEvent();
        public SimpleDropdownIndexEvent OnIndexSelected => _onIndexSelected;

        [SerializeField] [Tooltip("Dropdown callback event for selection by option")]
        private SimpleDropdownOptionEvent _onOptionSelected = new SimpleDropdownOptionEvent();
        public SimpleDropdownOptionEvent OnOptionSelected => _onOptionSelected;

        /// <summary>
        /// Whether the popup is currently showing or not
        /// </summary>
        public bool IsShowing { get; private set; }

        // Data currently being used
        private List<Toggle> _cells = new List<Toggle>();

        /// <summary>
        /// On awake, find dropdown cell
        /// </summary>
        protected virtual void Awake()
        {
            FindCellPrefab();
        }

        // Find existing toggle if no prefab is assigned
        private void FindCellPrefab()
        {
            if (_dropdownListCellPrefab == null)
            {
                _dropdownListCellPrefab = _dropdownListScrollRect.content.GetComponentInChildren<Toggle>();
                if (_dropdownListCellPrefab != null)
                {
                    _dropdownListCellPrefab.gameObject.SetActive(false);
                }
                else
                {
                    Debug.LogError("No Dropdown Cell Prefab Found");
                }
            }
        }

        /// <summary>
        /// Perform dropdown setup methods
        /// </summary>
        protected virtual void Start()
        {
            // Hide
            SetShowing(false);
            // Load dropdown
            if (_options != null && _options.Length > 0)
            {
                LoadDropdown(Options);
            }
            // Force a selection refresh
            int index = _selectedOptionIndex;
            _selectedOptionIndex = index - 1;
            SelectOption(index);
        }

        /// <summary>
        /// Add toggle delegate
        /// </summary>
        protected virtual void OnEnable()
        {
            _dropdownToggle.onValueChanged.AddListener(OnToggleClick);
        }
        /// <summary>
        /// Remove toggle delegate
        /// </summary>
        protected virtual void OnDisable()
        {
            _dropdownToggle.onValueChanged.RemoveListener(OnToggleClick);
        }

        #if ENABLE_LEGACY_INPUT_MANAGER
        /// <summary>
        /// Hide if touch outside of the scroll rect & toggle button
        /// </summary>
        protected virtual void Update()
        {
            if (IsShowing && Input.GetMouseButtonDown(0) && _dropdownListScrollRect != null && _dropdownToggle != null)
            {
                Vector2 touchPosition = Input.mousePosition;
                if (!IsInRect(_dropdownListScrollRect.viewport, touchPosition)
                    && !IsInRect(_dropdownToggle.GetComponent<RectTransform>(), touchPosition))
                {
                    SetShowing(false);
                }
            }
        }
        // Check for rect
        private static bool IsInRect(RectTransform rectTransform, Vector2 touchPosition)
        {
            // No main camera or rect
            Camera cam = Camera.main;
            if (cam == null || rectTransform == null)
            {
                return false;
            }
            // Check mouse position screen point
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, touchPosition, cam);
        }
        #endif

        #region LOAD
        /// <summary>
        /// Load dropdown with specified options
        /// </summary>
        /// <param name="newOptions">The options to be used</param>
        public void LoadDropdown(string[] newOptions)
        {
            // Get options
            _options = newOptions;

            // Disable all previous cells
            foreach (var cell in _cells)
            {
                cell.gameObject.SetActive(false);
            }

            // Setup if needed, or log error
            FindCellPrefab();
            if (_dropdownListCellPrefab == null)
            {
                VLog.W($"Cannot load {gameObject.name} without a cell prefab");
                return;
            }

            // Iterate all options
            float height = 0f;
            int total = _options == null ? 0 : _options.Length;
            for (int c = 0; c < total; c++)
            {
                // Get existing cell if possible
                Toggle cell = c < _cells.Count ? _cells[c] : null;

                // Instantiate if needed
                if (cell == null)
                {
                    // Instantiate
                    cell = Instantiate(_dropdownListCellPrefab.gameObject).GetComponent<Toggle>();
                    cell.name = $"{_dropdownListCellPrefab.gameObject.name}_CELL_{c:000}";
                    cell.transform.SetParent(_dropdownListScrollRect.content, false);

                    // Override cell
                    if (c < _cells.Count)
                    {
                        _cells[c] = cell;
                    }
                    // Add new cell
                    else
                    {
                        _cells.Add(cell);
                    }
                }

                // Enable & load
                cell.gameObject.SetActive(true);
                LoadCell(cell, c, _options[c], ref height);
            }

            // Set height to content of scrolldown list
            _dropdownListScrollRect.content.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0f, height);
        }
        /// <summary>
        /// Load a specific cell
        /// </summary>
        /// <param name="index">Cell index</param>
        /// <returns>Height of cell</returns>
        protected virtual void LoadCell(Toggle cell, int index, string option, ref float y)
        {
            // Apply name to cell
            Text cellText = cell.GetComponentInChildren<Text>();
            if (cellText != null)
            {
                cellText.text = option;
            }

            // Apply callback to cell
            cell.onValueChanged.RemoveAllListeners();
            cell.isOn = false;
            cell.onValueChanged.AddListener((isSelected) => OnCellClick(index, isSelected));

            // Apply width to cell
            RectTransform cellRect = cell.GetComponent<RectTransform>();
            cellRect.anchorMin = new Vector2(0f, 1f);
            cellRect.anchorMax = new Vector2(1f, 1f);
            cellRect.pivot = new Vector2(0.5f, 1f);
            cellRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _dropdownListScrollRect.content.rect.width);
            cellRect.anchoredPosition = new Vector2(0f, -y);

            // Apply height
            float height = cellRect.rect.height;
            if (cellText != null)
            {
                float textMargin = Mathf.Max(0f, height - cellText.rectTransform.rect.height);
                height = Mathf.CeilToInt(cellText.preferredHeight + _dropdownCellTextPadding + textMargin);
                cellRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }

            // Add new height
            y += height;
        }
        #endregion

        #region SELECTION
        /// <summary>
        /// Select an item in the dropdown list
        /// </summary>
        /// <param name="newIndex">The option index to be selected</param>
        public void SelectOption(int newIndex)
        {
            // Ignore if same
            if (newIndex == SelectedOptionIndex)
            {
                return;
            }

            // Apply new index
            _selectedOptionIndex = newIndex;

            // Get option text
            string optionText = SelectedOption;

            // Apply to button
            Text buttonText = _dropdownToggle.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                if (string.IsNullOrEmpty(optionText))
                {
                    buttonText.text = DropdownToggleUnselectedText;
                }
                else
                {
                    buttonText.text = optionText;
                }
            }

            // Adjust cells
            int total = _cells == null ? 0 : _cells.Count;
            for (int i = 0; i < total; i++)
            {
                _cells[i].isOn = i == SelectedOptionIndex;
                _cells[i].interactable = i != SelectedOptionIndex;
            }

            // Call event
            OnOptionSelected?.Invoke(optionText);
            OnIndexSelected?.Invoke(SelectedOptionIndex);

            // Selected
            if (IsShowing)
            {
                SetShowing(false);
            }
        }
        /// <summary>
        /// Select option by name
        /// </summary>
        /// <param name="option">The option to be selected</param>
        public void SelectOption(string option) => SelectOption(GetOptionIndex(option));
        /// <summary>
        /// When a cell is clicked, select a specific option
        /// </summary>
        /// <param name="index">Cell index clicked</param>
        /// <param name="toSelected">Whether cell was selected or unselected</param>
        private void OnCellClick(int index, bool toSelected)
        {
            if (toSelected && index != SelectedOptionIndex)
            {
                SelectOption(index);
            }
        }
        /// <summary>
        /// A method for obtaining the index of a dropdown option string.
        /// </summary>
        /// <param name="option">Accepts an option text string that matches a loaded text option.</param>
        /// <returns>Returns the index of the provided option string</returns>
        public int GetOptionIndex(string option)
        {
            if (_options != null)
            {
                for (int o = 0; o < _options.Length; o++)
                {
                    if (string.Equals(_options[o], option))
                    {
                        return o;
                    }
                }
            }
            return -1;
        }
        #endregion

        #region APPEARANCE
        /// <summary>
        /// Show or hide the popup
        /// </summary>
        /// <param name="toShowing">The option index to be selected</param>
        public void SetShowing(bool toShowing)
        {
            if (toShowing && (Options == null || Options.Length <= 0))
            {
                Debug.Log("SimpleDropdownList - Cannot show without any options");
                return;
            }
            IsShowing = toShowing;
            _dropdownToggle.isOn = IsShowing;
            if (_dropdownListPopup != null)
            {
                _dropdownListPopup.alpha = toShowing ? 1f : 0f;
                _dropdownListPopup.interactable = toShowing;
                _dropdownListPopup.blocksRaycasts = toShowing;
            }
            if (_dropdownButtonArrowImage != null)
            {
                _dropdownButtonArrowImage.localRotation = Quaternion.Euler(0f, 0f, IsShowing ? 0f : 180f);
            }
        }
        /// <summary>
        /// Called when toggle button is clicked
        /// </summary>
        private void OnToggleClick(bool toToggle)
        {
            if (toToggle != IsShowing)
            {
                SetShowing(toToggle);
            }
        }
        #endregion
    }
}
