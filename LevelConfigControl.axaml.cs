using Avalonia.Controls;
using Avalonia.Interactivity;
using HRCProgramV3;
using System;
using System.Text.RegularExpressions;

namespace HRCProgramV3
{
    public partial class LevelConfigControl : UserControl
    {
        private int _levelNumber;
        private bool _isFirstLevel;

        public LevelConfigControl(int levelNumber, bool isFirstLevel)
        {
            InitializeComponent();

            _levelNumber = levelNumber;
            _isFirstLevel = isFirstLevel;

            LevelHeaderText.Text = $"Level {levelNumber} Configuration";

            // Make radio button group unique for this level
            string uniqueGroupName = $"IdentifierMode_Level{levelNumber}";
            UseCaptureGroupRadio.GroupName = uniqueGroupName;
            UseAutoIncrementRadio.GroupName = uniqueGroupName;

            // Show asterisk panel only for first level
            if (isFirstLevel)
            {
                AsteriskPanel.IsVisible = true;
            }
        }

        private void IdentifierModeChanged(object? sender, RoutedEventArgs e)
        {
            if (AutoIncrementPanel != null && UseAutoIncrementRadio != null)
            {
                AutoIncrementPanel.IsVisible = UseAutoIncrementRadio.IsChecked == true;
            }
        }

        public LevelConfig? GetConfiguration()
        {
            // Clear any previous validation message
            ValidationMessage.IsVisible = false;

            // Validate regex
            string regexPattern = RegexTextBox.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(regexPattern))
            {
                ShowValidation("RegEx pattern is required.");
                return null;
            }

            // Ensure pattern is anchored at line start
            string anchoredPattern = regexPattern.StartsWith("^") ? regexPattern : "^" + regexPattern;

            // Try to compile regex and count groups
            Regex? regexObject;
            int captureGroupCount;
            string errorMessage;

            if (!TryCompileAndCountGroups(anchoredPattern, out regexObject, out captureGroupCount, out errorMessage))
            {
                ShowValidation($"Invalid regex: {errorMessage}");
                return null;
            }

            if (captureGroupCount != 1)
            {
                ShowValidation("RegEx must contain exactly one capturing group.");
                return null;
            }

            // Validate item type name
            string itemTypeName = ItemTypeTextBox.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(itemTypeName))
            {
                ShowValidation("Item type name is required.");
                return null;
            }

            // Validate asterisks for first level
            string asteriskNumber = "";
            if (_isFirstLevel)
            {
                asteriskNumber = AsteriskTextBox.Text?.Trim() ?? "";
                if (!ValidateAsterisk(asteriskNumber))
                {
                    ShowValidation("Please enter one or more asterisks (*).");
                    return null;
                }
            }

            // Build configuration
            LevelConfig config = new LevelConfig
            {
                LevelIndex = _levelNumber,
                OriginalPatternString = regexPattern,
                AnchoredPatternString = anchoredPattern,
                RegexObject = regexObject,
                ItemTypeName = itemTypeName,
                AsteriskNumber = asteriskNumber,
                UseCaptureGroup = UseCaptureGroupRadio.IsChecked == true,
                IsAutoIncrement = UseAutoIncrementRadio.IsChecked == true
            };

            // If auto-increment, set the kind and value
            if (config.IsAutoIncrement)
            {
                int selectedIndex = IncrementorComboBox.SelectedIndex;
                switch (selectedIndex)
                {
                    case 0: // Numeric
                        config.AutoStartKind = "numeric";
                        config.AutoStartValue = "1";
                        break;
                    case 1: // Uppercase
                        config.AutoStartKind = "alphaUpper";
                        config.AutoStartValue = "A";
                        break;
                    case 2: // Lowercase
                        config.AutoStartKind = "alphaLower";
                        config.AutoStartValue = "a";
                        break;
                    default:
                        config.AutoStartKind = "numeric";
                        config.AutoStartValue = "1";
                        break;
                }
            }

            return config;
        }

        private void ShowValidation(string message)
        {
            ValidationMessage.Text = message;
            ValidationMessage.IsVisible = true;
        }

        private bool TryCompileAndCountGroups(string anchoredPattern, out Regex? regexObj, out int captureGroupCount, out string errorMessage)
        {
            regexObj = null;
            captureGroupCount = 0;
            errorMessage = "";

            try
            {
                regexObj = new Regex(anchoredPattern, RegexOptions.Compiled);
                int[] groupNumbers = regexObj.GetGroupNumbers();
                if (groupNumbers != null)
                {
                    captureGroupCount = groupNumbers.Length - 1;
                }
                else
                {
                    captureGroupCount = 0;
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                captureGroupCount = 0;
                return false;
            }
        }

        private bool ValidateAsterisk(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            foreach (char c in input)
            {
                if (c != '*')
                {
                    return false;
                }
            }

            return true;
        }
    }
}