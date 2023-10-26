/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using UnityEditor;
using Meta.WitAi.Data.Configuration;

namespace Meta.WitAi.Windows
{
    public static class WitWindowUtility
    {
        // Window types
        public static Type SetupWindowType => FindChildClass<WitWelcomeWizard>();
        public static Type ConfigurationWindowType => FindChildClass<WitWindow>();
        public static Type UnderstandingWindowType => FindChildClass<WitUnderstandingViewer>();

        // Finds a child class if possible
        private static Type FindChildClass<T>()
        {
            // Find all subclasses & return the first
            List<Type> results = typeof(T).GetSubclassTypes(true);
            if (results != null && results.Count > 0)
            {
                return results[0];
            }

            // Return type passed in
            return typeof(T);
        }

        // Opens Setup Window
        public static void OpenGettingStarted(Action<WitConfiguration> onSetupComplete)
        {
            // Get wizard (Title is overwritten)
            WitWelcomeWizard wizard = (WitWelcomeWizard)ScriptableWizard.DisplayWizard(WitTexts.Texts.SetupTitleLabel, SetupWindowType, WitTexts.Texts.SetupSubmitButtonLabel);
            // Set success callback
            wizard.successAction = onSetupComplete;
        }

        // Opens Setup Window
        public static void OpenSetupWindow(Action<WitConfiguration> onSetupComplete)
        {
            // Get wizard (Title is overwritten)
            WitWelcomeWizard wizard = (WitWelcomeWizard)ScriptableWizard.DisplayWizard(WitTexts.Texts.SetupTitleLabel, SetupWindowType, WitTexts.Texts.SetupSubmitButtonLabel);
            // Set success callback
            wizard.successAction = onSetupComplete;
        }
        // Opens Configuration Window
        public static void OpenConfigurationWindow(WitConfiguration configuration = null)
        {
            // Setup if needed
            if (configuration == null && !WitConfigurationUtility.HasValidCustomConfig())
            {
                OpenSetupWindow(OpenConfigurationWindow);
                return;
            }

            // Get window & show
            WitConfigurationWindow window = (WitConfigurationWindow)EditorWindow.GetWindow(ConfigurationWindowType);
            window.autoRepaintOnSceneChange = true;
            window.SetConfiguration(configuration);
            window.Show();
        }
        // Opens Understanding Window to specific configuration
        public static void OpenUnderstandingWindow(WitConfiguration configuration = null)
        {
            // Setup if needed
            if (configuration == null && !WitConfigurationUtility.HasValidCustomConfig())
            {
                OpenSetupWindow(OpenUnderstandingWindow);
                return;
            }

            // Get window & show
            WitConfigurationWindow window = (WitConfigurationWindow)EditorWindow.GetWindow(UnderstandingWindowType);
            window.autoRepaintOnSceneChange = true;
            window.SetConfiguration(configuration);
            window.Show();
        }
    }
}
