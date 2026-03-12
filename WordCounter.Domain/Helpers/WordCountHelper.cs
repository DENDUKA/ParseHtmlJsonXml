using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WordCounter.Domain.Helpers;

public static class WordCountHelper
{
    public static void AnalyzeText(string text, Dictionary<string, int> frequencies, SearchValues<char> delimiters)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var lookup = frequencies.GetAlternateLookup<ReadOnlySpan<char>>();
        AnalyzeText(text.AsSpan(), lookup, frequencies, delimiters);
    }

    public static void AnalyzeText(ReadOnlySpan<char> text, Dictionary<string, int>.AlternateLookup<ReadOnlySpan<char>> lookup, Dictionary<string, int> frequencies, SearchValues<char> delimiters)
    {
        int processed = AnalyzeChunk(text, lookup, frequencies, delimiters);
        if (processed < text.Length)
        {
            CountWord(text.Slice(processed), lookup, frequencies);
        }
    }

    public static int AnalyzeChunk(ReadOnlySpan<char> text, Dictionary<string, int>.AlternateLookup<ReadOnlySpan<char>> lookup, Dictionary<string, int> frequencies, SearchValues<char> delimiters)
    {
        int start = 0;
        bool inWord = false;
        int lastWordStart = 0;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (delimiters.Contains(c))
            {
                if (inWord)
                {
                    CountWord(text.Slice(start, i - start), lookup, frequencies);
                    inWord = false;
                }
            }
            else
            {
                if (!inWord)
                {
                    start = i;
                    inWord = true;
                    lastWordStart = start;
                }
            }
        }

        return inWord ? lastWordStart : text.Length;
    }

    public static void CountWord(ReadOnlySpan<char> wordSpan, Dictionary<string, int>.AlternateLookup<ReadOnlySpan<char>> lookup, Dictionary<string, int> frequencies)
    {
        // Avoid allocation if word exists
        if (lookup.TryGetValue(wordSpan, out int count))
        {
            lookup[wordSpan] = count + 1;
        }
        else
        {
            // Only allocate if new word
            string word = wordSpan.ToString().ToLowerInvariant();
            if (frequencies.TryGetValue(word, out count))
            {
                 frequencies[word] = count + 1;
            }
            else
            {
                frequencies.Add(word, 1);
            }
        }
    }
}
