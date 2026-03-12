using System.Buffers;
using System.Text;
using WordCounter.Domain.Helpers;
using WordCounter.Domain.Interfaces;
using WordCounter.Domain.Models;

namespace WordCounter.Infrastructure.Counters;

public class HtmlWordCounter(WordCounterSettings settings) : IWordCounter
{
    private const int BufferSize = 4096;
    const string ScriptEnd = "</script>";
    const string StyleEnd = "</style>";

    private readonly SearchValues<char> _delimiters = SearchValues.Create([.. settings.Delimiters.Select(s => s[0])]);

    private enum Mode { Text, InTag, InScript, InStyle }

    public async Task<Dictionary<string, int>> CountWords(Stream stream, CancellationToken ct)
    {
        var frequencies = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lookup = frequencies.GetAlternateLookup<ReadOnlySpan<char>>();

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: BufferSize, leaveOpen: true);

        char[] buffer = ArrayPool<char>.Shared.Rent(BufferSize);
        char[] wordBuffer = ArrayPool<char>.Shared.Rent(256);
        char[] tagNameBuffer = new char[16];

        try
        {
            int charsRead;
            int wordLen = 0;
            int tagNameLen = 0;
            bool tagNameComplete = false;
            int endTagMatchIndex = 0;
            Mode mode = Mode.Text;

            while ((charsRead = await reader.ReadAsync(buffer, ct)) > 0)
            {
                ReadOnlySpan<char> span = buffer.AsSpan(0, charsRead);

                for (int i = 0; i < span.Length; i++)
                {
                    char c = span[i];

                    if (mode == Mode.InScript)
                    {
                        CheckEndTag(c, ScriptEnd, ref endTagMatchIndex, ref mode);
                    }
                    else if (mode == Mode.InStyle)
                    {
                        CheckEndTag(c, StyleEnd, ref endTagMatchIndex, ref mode);
                    }
                    else if (mode == Mode.InTag)
                    {
                        if (c == '>')
                        {
                            if (IsTag(tagNameBuffer, tagNameLen, "script")) mode = Mode.InScript;
                            else if (IsTag(tagNameBuffer, tagNameLen, "style")) mode = Mode.InStyle;
                            else mode = Mode.Text;

                            endTagMatchIndex = 0;
                        }
                        else
                        {
                            if (!tagNameComplete)
                            {
                                if (char.IsWhiteSpace(c) || c == '/')
                                {
                                    tagNameComplete = true;
                                }
                                else if (tagNameLen < tagNameBuffer.Length)
                                {
                                    tagNameBuffer[tagNameLen++] = c;
                                }
                                else
                                {
                                    tagNameComplete = true;
                                }
                            }
                        }
                    }
                    else // Mode.Text
                    {
                        if (c == '<')
                        {
                            if (wordLen > 0)
                            {
                                WordCountHelper.CountWord(wordBuffer.AsSpan(0, wordLen), lookup, frequencies);
                                wordLen = 0;
                            }
                            mode = Mode.InTag;
                            tagNameLen = 0;
                            tagNameComplete = false;
                        }
                        else
                        {
                            ProcessChar(c, lookup, frequencies, ref wordBuffer, ref wordLen);
                        }
                    }
                }
            }

            if (mode == Mode.Text && wordLen > 0)
            {
                WordCountHelper.CountWord(wordBuffer.AsSpan(0, wordLen), lookup, frequencies);
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
            ArrayPool<char>.Shared.Return(wordBuffer);
        }

        return frequencies;
    }

    private void ProcessChar(char c, Dictionary<string, int>.AlternateLookup<ReadOnlySpan<char>> lookup, Dictionary<string, int> frequencies, ref char[] wordBuffer, ref int wordLen)
    {
        if (_delimiters.Contains(c))
        {
            if (wordLen > 0)
            {
                WordCountHelper.CountWord(wordBuffer.AsSpan(0, wordLen), lookup, frequencies);
                wordLen = 0;
            }
        }
        else
        {
            if (wordLen >= wordBuffer.Length)
                GrowBuffer(ref wordBuffer);
            wordBuffer[wordLen++] = c;
        }
    }

    private static void GrowBuffer(ref char[] buffer)
    {
        char[] newBuffer = ArrayPool<char>.Shared.Rent(buffer.Length * 2);
        buffer.AsSpan().CopyTo(newBuffer);
        ArrayPool<char>.Shared.Return(buffer);
        buffer = newBuffer;
    }

    private static void CheckEndTag(char c, string target, ref int matchIndex, ref Mode mode)
    {
        if (char.ToLowerInvariant(c) == target[matchIndex])
        {
            matchIndex++;
            if (matchIndex == target.Length)
            {
                mode = Mode.Text;
                matchIndex = 0;
            }
        }
        else
        {
            matchIndex = (c == '<') ? 1 : 0;
        }
    }

    private static bool IsTag(char[] buffer, int len, string target)
    {
        if (len != target.Length) return false;
        for (int i = 0; i < len; i++)
            if (char.ToLowerInvariant(buffer[i]) != target[i]) return false;
        return true;
    }
}
