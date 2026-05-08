using System;
using System.Collections.Generic;
using Rampastring.XNAUI.FontManagement;

namespace Rampastring.XNAUI;

public class TextParseReturnValue
{
    public int LineCount;
    public string Text;

    public TextParseReturnValue(string text, int lineCount)
    {
        Text = text;
        LineCount = lineCount;
    }

    public static TextParseReturnValue FixText(IFont font, int width, string text)
    {
        string line = string.Empty;
        int lineCount = 0;
        string processedText = string.Empty;
        string[] wordArray = text.Split(' ');

        foreach (string word in wordArray)
        {
            if (font.MeasureString(line + word).X > width)
            {
                processedText = processedText + line + Environment.NewLine;
                lineCount++;
                line = string.Empty;
            }

            line = line + word + " ";
        }

        processedText = processedText + line;
        return new TextParseReturnValue(processedText, lineCount);
    }

    public static List<string> GetFixedTextLines(IFont font, int width, string text, bool splitWords = true, bool keepBlankLines = false)
    {
        if (string.IsNullOrEmpty(text))
            return new List<string>(0);

        var returnValue = new List<string>();

        // Remove '\r' characters so Windows newlines aren't counted twice
        string[] lineArray = text.Replace("\r", "").Split(new char[] { '\n' }, StringSplitOptions.None);

        foreach (string originalTextLine in lineArray)
        {
            if (keepBlankLines && originalTextLine == string.Empty)
                returnValue.Add(string.Empty);

            string line = string.Empty;

            string[] wordArray = originalTextLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string word in wordArray)
            {
                if (font.MeasureString(line + word).X > width)
                {
                    if (line.Length > 0)
                    {
                        returnValue.Add(line.Remove(line.Length - 1));
                    }

                    // Split individual words that are longer than the allowed width
                    if (splitWords && font.MeasureString(word).X > width)
                    {
                        int start = 0;
                        while (start < word.Length)
                        {
                            int remaining = word.Length - start;
                            int low = 0, high = remaining;
                            while (low < high)
                            {
                                int mid = (low + high + 1) / 2;
                                if (font.MeasureString(word.Substring(start, mid)).X <= width)
                                    low = mid;
                                else
                                    high = mid - 1;
                            }
                            if (low >= remaining)
                                break;
                            // Snap to code point boundary to avoid splitting a surrogate pair.
                            // If no code point fits, force one through to avoid an infinite loop.
                            if (low > 0 && char.IsHighSurrogate(word[start + low - 1]))
                                low--;
                            if (low == 0)
                                low = char.IsSurrogatePair(word, start) ? 2 : 1;
                            returnValue.Add(word.Substring(start, low));
                            start += low;
                        }

                        line = word.Substring(start) + " ";
                        continue;
                    }

                    line = word + " ";
                    continue;
                }

                line = line + word + " ";
            }

            if (!string.IsNullOrEmpty(line) && line.Length > 1)
                returnValue.Add(line.TrimEnd());
        }

        return returnValue;
    }
}
