namespace Rampastring.XNAUI.Extensions;

/// <summary>
/// Extension methods for <see cref="string"/>.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Returns a copy of this string truncated to at most <paramref name="maxLength"/> UTF-16 code units, ensuring the result does not end with an orphaned high surrogate.
    /// </summary>
    /// <param name="str">The string to truncate.</param>
    /// <param name="maxLength">Maximum number of UTF-16 code units to include in the result. Must be non-negative.</param>
    /// <returns>The truncated string, or the original string if it is already short enough.</returns>
    public static string SubstringSurrogateAware(this string str, int maxLength)
    {
        if (str.Length <= maxLength)
            return str;
        if (maxLength > 0 && char.IsHighSurrogate(str[maxLength - 1]))
            maxLength--;
        return str.Substring(0, maxLength);
    }

    /// <summary>
    /// Returns a substring of this string starting at <paramref name="start"/> and containing at most
    /// <paramref name="maxLength"/> UTF-16 code units, ensuring the result does not end with an orphaned high surrogate.
    /// </summary>
    /// <param name="str">The string to slice.</param>
    /// <param name="start">The zero-based start index of the substring.</param>
    /// <param name="maxLength">Maximum number of UTF-16 code units to include in the result. Must be non-negative.</param>
    /// <returns>The safe substring.</returns>
    public static string SubstringSurrogateAware(this string str, int start, int maxLength)
    {
        int available = str.Length - start;
        int length = maxLength < available ? maxLength : available;
        if (length > 0 && char.IsHighSurrogate(str[start + length - 1]))
            length--;
        return str.Substring(start, length);
    }
}
