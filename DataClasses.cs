using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HRCProgramV3
{
    public class LevelConfig
    {
        public int LevelIndex { get; set; } = 0;
        public string OriginalPatternString { get; set; } = "";
        public string AnchoredPatternString { get; set; } = "";
        public string ItemTypeName { get; set; } = "";
        public string AsteriskNumber { get; set; } = "";
        public Regex RegexObject { get; set; } = new Regex("$^", RegexOptions.Compiled);

        public bool UseCaptureGroup { get; set; } = false;
        public bool IsAutoIncrement { get; set; } = false;

        public string AutoStartKind { get; set; } = "";
        public string AutoStartValue { get; set; } = "";
    }

    public class HierarchyState
    {
        public string?[] CurrentIds { get; set; } = Array.Empty<string?>();
        public string?[] AutoCounters { get; set; } = Array.Empty<string?>();
        public string?[] ParentSignatureForLevel { get; set; } = Array.Empty<string?>();
    }

    public class ProcessingSummary
    {
        public int TotalLines { get; set; } = 0;
        public int TransformedLinesCount { get; set; } = 0;
        public int SkippedOutOfOrderCount { get; set; } = 0;
        public int MultiMatchWarningCount { get; set; } = 0;
        public List<int> LevelsWithZeroMatches { get; set; } = new List<int>();
    }

    public class ProcessingResult
    {
        public List<string> OutputLines { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public ProcessingSummary Summary { get; set; } = new ProcessingSummary();
    }
}