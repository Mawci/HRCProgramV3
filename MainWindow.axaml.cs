using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using HRCProgramV3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRCProgramV3
{
    public partial class MainWindow : Window
    {
        private string? _selectedFilePath;
        private List<LevelConfigControl> _levelControls = new List<LevelConfigControl>();

        public MainWindow()
        {
            InitializeComponent();
            FilePathTextBox.TextChanged += FilePathTextBox_TextChanged;
        }

        private async void BrowseButton_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Markdown File",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Markdown Files")
                    {
                        Patterns = new[] { "*.md", "*.markdown" }
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (files.Count > 0)
            {
                _selectedFilePath = files[0].Path.LocalPath;
                FilePathTextBox.Text = _selectedFilePath;
                UpdateProcessButtonState();
                UpdateStatus("File selected: " + Path.GetFileName(_selectedFilePath));
            }
        }

        private void GenerateConfigButton_Click(object? sender, RoutedEventArgs e)
        {
            int numberOfLevels = (int)NumberOfLevelsInput.Value;

            LevelConfigPanel.Children.Clear();
            _levelControls.Clear();

            for (int i = 1; i <= numberOfLevels; i++)
            {
                var levelControl = new LevelConfigControl(i, i == 1);
                _levelControls.Add(levelControl);
                LevelConfigPanel.Children.Add(levelControl);
            }

            UpdateProcessButtonState();
            UpdateStatus($"Generated configuration for {numberOfLevels} level(s). Please fill in the details.");
        }

        private async void ProcessButton_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFilePath) || !File.Exists(_selectedFilePath))
            {
                UpdateStatus("Error: Please select a valid file.");
                return;
            }

            if (_levelControls.Count == 0)
            {
                UpdateStatus("Error: Please generate level configuration first.");
                return;
            }

            // Validate all level configurations
            List<LevelConfig> levelConfigs = new List<LevelConfig>();
            for (int i = 0; i < _levelControls.Count; i++)
            {
                var control = _levelControls[i];
                var config = control.GetConfiguration();

                if (config == null)
                {
                    UpdateStatus($"Error: Level {i + 1} configuration is invalid. Please check all fields.");
                    return;
                }

                levelConfigs.Add(config);
            }

            try
            {
                UpdateStatus("Processing file...");
                ProcessButton.IsEnabled = false;

                // Read the file
                string[] inputLinesArray = await File.ReadAllLinesAsync(_selectedFilePath, Encoding.UTF8);
                List<string> inputLines = new List<string>(inputLinesArray);

                // Process the lines using the existing logic
                ProcessingResult result = HRCProcessor.ProcessLines(inputLines, levelConfigs);

                // Write output
                string outputPath = BuildOutputPath(_selectedFilePath);
                await WriteOutputFileAsync(outputPath, result.OutputLines);

                // Show summary
                string summary = BuildSummaryText(result, outputPath);
                UpdateStatus(summary);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error processing file: {ex.Message}");
            }
            finally
            {
                ProcessButton.IsEnabled = true;
            }
        }

        private string BuildOutputPath(string inputPath)
        {
            string? directory = Path.GetDirectoryName(inputPath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputPath);
            string extension = Path.GetExtension(inputPath) ?? ".md";

            string outputName = fileNameWithoutExtension + ".processed" + extension;
            string outputPath = Path.Combine(directory ?? "", outputName);
            return outputPath;
        }

        private async Task WriteOutputFileAsync(string outputPath, List<string> outputLines)
        {
            string content = string.Join("\r\n", outputLines);
            UTF8Encoding utf8WithBom = new UTF8Encoding(true);
            await File.WriteAllTextAsync(outputPath, content, utf8WithBom);
        }

        private string BuildSummaryText(ProcessingResult result, string outputPath)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("✓ Processing Complete!");
            sb.AppendLine();
            sb.AppendLine($"Output file: {outputPath}");
            sb.AppendLine($"Total lines: {result.Summary.TotalLines}");
            sb.AppendLine($"Lines altered: {result.Summary.TransformedLinesCount}");
            sb.AppendLine($"Skipped (out-of-order) lines: {result.Summary.SkippedOutOfOrderCount}");
            sb.AppendLine($"Lines with multiple matches: {result.Summary.MultiMatchWarningCount}");

            if (result.Summary.LevelsWithZeroMatches != null && result.Summary.LevelsWithZeroMatches.Count > 0)
            {
                sb.AppendLine();
                sb.Append("⚠ Warning: The following levels had zero matches: ");
                sb.AppendLine(string.Join(", ", result.Summary.LevelsWithZeroMatches));
            }

            if (result.Warnings != null && result.Warnings.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Detailed warnings:");
                foreach (var warning in result.Warnings)
                {
                    sb.AppendLine($" - {warning}");
                }
            }

            return sb.ToString();
        }

        private void UpdateStatus(string message)
        {
            StatusTextBlock.Text = message;
        }

        private void UpdateProcessButtonState()
        {
            ProcessButton.IsEnabled = !string.IsNullOrEmpty(_selectedFilePath) &&
                                      File.Exists(_selectedFilePath) &&
                                      _levelControls.Count > 0;
        }

        private void FilePathTextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            string path = FilePathTextBox.Text?.Trim() ?? "";

            // Remove surrounding quotes if pasted with quotes
            if (path.StartsWith("\"") && path.EndsWith("\"") && path.Length >= 2)
            {
                path = path.Substring(1, path.Length - 2);
                FilePathTextBox.Text = path;
            }

            // Validate the file exists
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                _selectedFilePath = path;
                UpdateProcessButtonState();
                UpdateStatus("File selected: " + Path.GetFileName(path));
            }
            else if (!string.IsNullOrEmpty(path))
            {
                _selectedFilePath = null;
                UpdateProcessButtonState();
                UpdateStatus("Invalid file path. Please select a valid file.");
            }
        }
    }
}