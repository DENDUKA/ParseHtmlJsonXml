using System.Buffers;
using System.Xml;
using Microsoft.Extensions.Logging;
using WordCounter.Domain.Helpers;
using WordCounter.Domain.Interfaces;
using WordCounter.Domain.Models;

namespace WordCounter.Infrastructure.Counters;

public class XmlWordCounter(WordCounterSettings settings, ILogger<XmlWordCounter> logger) : IWordCounter
{
    private readonly SearchValues<char> _delimiters = SearchValues.Create([.. settings.Delimiters.Select(s => s[0])]);

    public async Task<Dictionary<string, int>> CountWords(Stream stream, CancellationToken ct)
    {
        var frequencies = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lookup = frequencies.GetAlternateLookup<ReadOnlySpan<char>>();
        
        var settings = new XmlReaderSettings 
        {
            Async = true,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true,
            DtdProcessing = DtdProcessing.Prohibit
        };

        char[] buffer = ArrayPool<char>.Shared.Rent(4096);
        int bufferOffset = 0;

        try 
        {
            using var reader = XmlReader.Create(stream, settings);
            
            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA)
                {
                    int charsRead;
                    while (true)
                    {
                        if (bufferOffset == buffer.Length)
                        {
                            var newBuffer = ArrayPool<char>.Shared.Rent(buffer.Length * 2);
                            buffer.AsSpan(0, bufferOffset).CopyTo(newBuffer);
                            ArrayPool<char>.Shared.Return(buffer);
                            buffer = newBuffer;
                        }

                        charsRead = await reader.ReadValueChunkAsync(buffer, bufferOffset, buffer.Length - bufferOffset);
                        if (charsRead == 0) break;

                        int totalChars = bufferOffset + charsRead;
                        var span = buffer.AsSpan(0, totalChars);
                        
                        int processed = WordCountHelper.AnalyzeChunk(span, lookup, frequencies, _delimiters);
                        
                        if (processed < totalChars)
                        {
                            int leftover = totalChars - processed;
                            span.Slice(processed).CopyTo(buffer.AsSpan(0, leftover));
                            bufferOffset = leftover;
                        }
                        else
                        {
                            bufferOffset = 0;
                        }
                    }

                    if (bufferOffset > 0)
                    {
                        WordCountHelper.AnalyzeText(buffer.AsSpan(0, bufferOffset), lookup, frequencies, _delimiters);
                        bufferOffset = 0;
                    }
                }
            }
        }
        catch (XmlException ex)
        {
             logger.LogError(ex, "XML Parsing Error. Line {Line}, Pos {Pos}", ex.LineNumber, ex.LinePosition);
             throw;
        }
        catch (Exception ex)
        {
             logger.LogError(ex, "Unexpected error in XML counter");
             throw;
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }

        return frequencies;
    }
}
