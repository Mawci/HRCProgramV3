using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace HRCProgramV3
{
    public static class HRCProcessor
    {
        public static ProcessingResult ProcessLines(List<string> inputLines, List<LevelConfig> levelConfigs)
        {
            ProcessingResult result = new ProcessingResult();
            result.OutputLines = new List<string>();
            result.Warnings = new List<string>();
            result.Summary = new ProcessingSummary();

            result.Summary.TotalLines = inputLines.Count;
            result.Summary.TransformedLinesCount = 0;
            result.Summary.SkippedOutOfOrderCount = 0;
            result.Summary.MultiMatchWarningCount = 0;

            int numberOfLevels = levelConfigs.Count;

            HierarchyState state = new HierarchyState();
            state.CurrentIds = new string?[numberOfLevels + 1];
            state.AutoCounters = new string?[numberOfLevels + 1];
            state.ParentSignatureForLevel = new string?[numberOfLevels + 1];

            int[] levelMatchCounts = new int[numberOfLevels + 1];
            for (int i = 1; i <= numberOfLevels; i++)
            {
                levelMatchCounts[i] = 0;
                state.CurrentIds[i] = null;
                state.AutoCounters[i] = null;
                state.ParentSignatureForLevel[i] = null;
            }

            for (int lineIndex = 0; lineIndex < inputLines.Count; lineIndex++)
            {
                string line = inputLines[lineIndex];

                List<int> matchedLevels = new List<int>();
                List<Match> matchedMatches = new List<Match>();

                for (int level = 1; level <= numberOfLevels; level++)
                {
                    Regex rx = levelConfigs[level - 1].RegexObject;
                    Match m = rx.Match(line);
                    if (m.Success)
                    {
                        matchedLevels.Add(level);
                        matchedMatches.Add(m);
                    }
                }

                if (matchedLevels.Count == 0)
                {
                    result.OutputLines.Add(line);
                    continue;
                }

                if (matchedLevels.Count > 1)
                {
                    result.Summary.MultiMatchWarningCount = result.Summary.MultiMatchWarningCount + 1;
                    string warn = "Line " + (lineIndex + 1) + " matched multiple levels: ";
                    for (int i = 0; i < matchedLevels.Count; i++)
                    {
                        warn = warn + matchedLevels[i];
                        if (i < matchedLevels.Count - 1)
                        {
                            warn = warn + ", ";
                        }
                    }
                    result.Warnings.Add(warn);
                }

                int chosenLevel = matchedLevels[0];
                Match chosenMatch = matchedMatches[0];

                LevelConfig config = levelConfigs[chosenLevel - 1];
                string identifierForThisLevel = "";

                if (config.UseCaptureGroup)
                {
                    identifierForThisLevel = chosenMatch.Groups[1].Value;
                }
                else if (config.IsAutoIncrement)
                {
                    string parentSig = BuildParentHRC(state.CurrentIds, chosenLevel);
                    string? lastSig = state.ParentSignatureForLevel[chosenLevel];

                    if (string.IsNullOrEmpty(lastSig) || parentSig != lastSig)
                    {
                        string resetValue = AutoCounterResetToBase(config.AutoStartKind, config.AutoStartValue);
                        state.AutoCounters[chosenLevel] = resetValue;
                        state.ParentSignatureForLevel[chosenLevel] = parentSig;
                    }
                    else
                    {
                        string currentCounter = state.AutoCounters[chosenLevel] ?? config.AutoStartValue;
                        string incremented = AutoCounterIncrement(currentCounter, config.AutoStartKind);
                        state.AutoCounters[chosenLevel] = incremented;
                    }

                    identifierForThisLevel = state.AutoCounters[chosenLevel] ?? config.AutoStartValue;
                }

                state.CurrentIds[chosenLevel] = identifierForThisLevel;
                for (int d = chosenLevel + 1; d <= numberOfLevels; d++)
                {
                    state.CurrentIds[d] = null;
                    state.AutoCounters[d] = null;
                    state.ParentSignatureForLevel[d] = null;
                }

                string compositeCode = BuildCompositeCode(state.CurrentIds, chosenLevel);

                int actualDepth = 0;
                for (int i = 1; i <= chosenLevel; i++)
                {
                    if (!string.IsNullOrEmpty(state.CurrentIds[i]))
                    {
                        actualDepth++;
                    }
                }

                int baseAsteriskCount = levelConfigs[0].AsteriskNumber.Length;
                int totalAsteriskCount = baseAsteriskCount + actualDepth - 1;
                string dynamicAsterisks = new string('*', totalAsteriskCount);

                int prefixLen = chosenMatch.Length;
                string remainder;
                if (prefixLen <= line.Length)
                {
                    remainder = line.Substring(prefixLen);
                }
                else
                {
                    remainder = "";
                }

                string newLine = config.ItemTypeName + " " + dynamicAsterisks + " :" + compositeCode + " " + remainder;

                result.OutputLines.Add(newLine);
                result.Summary.TransformedLinesCount = result.Summary.TransformedLinesCount + 1;
                levelMatchCounts[chosenLevel] = levelMatchCounts[chosenLevel] + 1;
            }

            List<int> zeroMatchLevels = new List<int>();
            for (int i = 1; i <= numberOfLevels; i++)
            {
                if (levelMatchCounts[i] == 0)
                {
                    zeroMatchLevels.Add(i);
                }
            }
            result.Summary.LevelsWithZeroMatches = zeroMatchLevels;

            return result;
        }

        private static string BuildParentHRC(string?[] currentIds, int level)
        {
            if (level <= 1)
            {
                return "";
            }

            List<string> parts = new List<string>();
            for (int i = 1; i <= level - 1; i++)
            {
                if (!string.IsNullOrEmpty(currentIds[i]))
                {
                    parts.Add(currentIds[i]!);
                }
            }

            string signature = JoinWithDot(parts);
            return signature;
        }

        private static string BuildCompositeCode(string?[] currentIds, int upToLevel)
        {
            List<string> parts = new List<string>();
            for (int i = 1; i <= upToLevel; i++)
            {
                if (!string.IsNullOrEmpty(currentIds[i]))
                {
                    parts.Add(currentIds[i]!);
                }
            }
            string composite = JoinWithDot(parts);
            return composite;
        }

        private static string JoinWithDot(List<string> parts)
        {
            if (parts == null || parts.Count == 0)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < parts.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(".");
                }
                sb.Append(parts[i]);
            }
            return sb.ToString();
        }

        private static string AutoCounterResetToBase(string kind, string baseCharString)
        {
            return baseCharString;
        }

        private static string AutoCounterIncrement(string currentToken, string kind)
        {
            if (kind == "numeric")
            {
                int number;
                bool ok = int.TryParse(currentToken, out number);
                if (!ok)
                {
                    number = 1;
                }
                number = number + 1;
                return number.ToString();
            }
            else if (kind == "alphaLower")
            {
                string incremented = IncrementAlphaToken(currentToken, false);
                return incremented;
            }
            else if (kind == "alphaUpper")
            {
                string incremented = IncrementAlphaToken(currentToken, true);
                return incremented;
            }
            else
            {
                return currentToken;
            }
        }

        private static string IncrementAlphaToken(string token, bool isUpper)
        {
            if (string.IsNullOrEmpty(token))
            {
                return isUpper ? "A" : "a";
            }

            char minChar = isUpper ? 'A' : 'a';
            char maxChar = isUpper ? 'Z' : 'z';

            char[] chars = token.ToCharArray();
            int index = chars.Length - 1;
            bool carry = true;

            while (index >= 0 && carry)
            {
                if (chars[index] == maxChar)
                {
                    chars[index] = minChar;
                    carry = true;
                    index = index - 1;
                }
                else
                {
                    chars[index] = (char)(chars[index] + 1);
                    carry = false;
                }
            }

            if (carry)
            {
                string newToken = new string(chars);
                return (isUpper ? "A" : "a") + newToken;
            }
            else
            {
                string newToken = new string(chars);
                return newToken;
            }
        }
    }
}