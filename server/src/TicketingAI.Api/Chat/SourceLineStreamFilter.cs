using System.Text;

namespace TicketingAI.Api.Chat;

/// <summary>
/// Holds back the leading "Source: ..." line of a streamed reply, so it is never shown, and lets everything after it through.
/// If the reply does not start with a source line, the text is let through as soon as that is clear.
/// </summary>
public class SourceLineStreamFilter
{
    private const string Marker = "Source:";

    private readonly StringBuilder _buffer = new();
    private bool _decided;
    private bool _skippingBlankLines;

    /// <summary>Takes the next piece of the reply and returns the text that is safe to show now (possibly nothing yet).</summary>
    public string Push(string chunk)
    {
        if (_decided)
            return _skippingBlankLines ? TrimBlankLines(chunk) : chunk;

        _buffer.Append(chunk);
        var text = _buffer.ToString().TrimStart();

        // Could still turn into "Source:", so keep waiting.
        if (text.Length < Marker.Length && Marker.StartsWith(text, StringComparison.OrdinalIgnoreCase))
            return "";

        if (!text.StartsWith(Marker, StringComparison.OrdinalIgnoreCase))
        {
            _decided = true;
            return _buffer.ToString();
        }

        var endOfLine = text.IndexOf('\n');
        if (endOfLine < 0)
            return ""; // the source line is still arriving

        // The blank line after the source line can arrive in a later piece, so keep skipping until real text shows up.
        _decided = true;
        _skippingBlankLines = true;
        return TrimBlankLines(text[(endOfLine + 1)..]);
    }

    private string TrimBlankLines(string text)
    {
        var trimmed = text.TrimStart('\r', '\n');
        if (trimmed.Length > 0)
            _skippingBlankLines = false;

        return trimmed;
    }
}
