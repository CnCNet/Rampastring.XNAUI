namespace Rampastring.XNAUI.Extensions;

/// <summary>
/// Extension methods for <see cref="string"/>.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Returns a copy of this string truncated to at most <paramref name="maxLength"/> characters,
    /// ensuring the result does not end with an orphaned high surrogate.
    /// </summary>
    /// <param name="str">The string to truncate.</param>
    /// <param name="maxLength">Maximum number of characters to include in the result. Must be non-negative.</param>
    /// <returns>The truncated string, or the original string if it is already short enough.</returns>
    public static string TruncateAtCharBoundary(this string str, int maxLength)
    {
        if (str.Length <= maxLength)
            return str;
        if (maxLength > 0 && char.IsHighSurrogate(str[maxLength - 1]))
            maxLength--;
        return str.Substring(0, maxLength);
    }
}
