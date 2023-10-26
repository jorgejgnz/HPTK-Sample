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
using UnityEngine;

/// <summary>
/// Represents the semantic classification of a <see cref="OVRSceneAnchor"/>.
/// </summary>
/// <remarks>
/// Scene anchors can have one or more string labels associated with them that describes what the anchor represents,
/// such as COUCH, or DESK. See <see cref="OVRSceneManager.Classification"/> for a list of possible labels.
/// </remarks>
[HelpURL("https://developer.oculus.com/reference/unity/latest/class_o_v_r_semantic_classification")]
[DisallowMultipleComponent]
[RequireComponent(typeof(OVRSceneAnchor))]
public class OVRSemanticClassification : MonoBehaviour, IOVRSceneComponent
{
    public const char LabelSeparator = ',';
    private readonly List<string> _labels = new List<string>();

    /// <summary>
    /// A list of labels associated with an <see cref="OVRSceneAnchor"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public IReadOnlyList<string> Labels => _labels;

    /// <summary>
    /// Searches <see cref="Labels"/> for the given <paramref name="label"/>.
    /// </summary>
    /// <remarks>
    /// This method performs a linear search over the <see cref="Labels"/>.
    /// </remarks>
    /// <param name="label">The label to find</param>
    /// <returns>Returns true if <paramref name="label"/> exists in <see cref="Labels"/>.</returns>
    public bool Contains(string label)
    {
        foreach (var item in _labels)
        {
            if (item == label)
            {
                return true;
            }
        }

        return false;
    }

    private void Awake()
    {
        if (GetComponent<OVRSceneAnchor>().Space.Valid)
        {
            ((IOVRSceneComponent)this).Initialize();
        }
    }

    void IOVRSceneComponent.Initialize()
    {
        if (OVRPlugin.GetSpaceSemanticLabels(GetComponent<OVRSceneAnchor>().Space, out var labels))
        {
            _labels.Clear();
            _labels.AddRange(ValidateAndUpgradeLabels(labels).Split(LabelSeparator));

            OVRSceneManager.Development.Log(nameof(OVRSemanticClassification),
                $"[{GetComponent<OVRSceneAnchor>().Uuid}] {nameof(OVRSceneAnchor)} has labels: {labels}.",
                gameObject);
        }
        else
        {
            OVRSceneManager.Development.LogWarning(nameof(OVRSemanticClassification),
                $"[{GetComponent<OVRSceneAnchor>().Uuid}] {nameof(OVRSceneAnchor)} has no semantic labels.",
                gameObject);
        }
    }

    /// <summary>
    /// Checks labels to ensure that we've accounted for upgraded labels,
    /// such as all Table labels also including Desk.
    /// All InvisibleWallFace labels also include WallFace.
    /// </summary>
    internal static string ValidateAndUpgradeLabels(string labels)
    {
        using (new OVRObjectPool.ListScope<string>(out var newLabels))
        {
            var splitLabels = labels.Split(LabelSeparator);
            var hasTable = false;
            var hasDesk = false;
            var hasInvisibleWallFace = false;
            var hasWallFace = false;

#pragma warning disable CS0618 // Type or member is obsolete
            // OpenXR will only return TABLE, but we support DESK as it's
            // Obsolete. This code will be removed once we remove DESK.
            foreach (var label in splitLabels)
            {
                newLabels.Add(label);

                if (label == OVRSceneManager.Classification.Table)
                    hasTable = true;
                else if (label == OVRSceneManager.Classification.Desk)
                    hasDesk = true;
                else if (label == OVRSceneManager.Classification.InvisibleWallFace)
                    hasInvisibleWallFace = true;
                else if (label == OVRSceneManager.Classification.WallFace)
                    hasWallFace = true;
            }

            if (hasTable && !hasDesk)
                newLabels.Add(OVRSceneManager.Classification.Desk);
            else if (hasDesk && !hasTable)
                newLabels.Add(OVRSceneManager.Classification.Table);
#pragma warning restore CS0618 // Type or member is obsolete

            if (hasInvisibleWallFace && !hasWallFace)
                newLabels.Add(OVRSceneManager.Classification.WallFace);

            return string.Join(LabelSeparator.ToString(), newLabels);
        }
    }
}
