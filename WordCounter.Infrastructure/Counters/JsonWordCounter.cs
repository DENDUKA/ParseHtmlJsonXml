using System.Buffers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WordCounter.Domain.Helpers;
using WordCounter.Domain.Interfaces;
using WordCounter.Domain.Models;

namespace WordCounter.Infrastructure.Counters;

public class JsonWordCounter(WordCounterSettings settings, ILogger<JsonWordCounter> logger) : IWordCounter
{
    private readonly SearchValues<char> _delimiters = SearchValues.Create([.. settings.Delimiters.Select(s => s[0])]);
    private const int InitialBufferSize = 1024 * 1024; // 1MB

    public async Task<Dictionary<string, int>> CountWords(Stream stream, CancellationToken ct)
    {
        var frequencies = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lookup = frequencies.GetAlternateLookup<ReadOnlySpan<char>>();

        byte[] buffer = ArrayPool<byte>.Shared.Rent(InitialBufferSize);
        int bytesBuffered = 0;
        var readerState = new JsonReaderState();

        try
        {
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer.AsMemory(bytesBuffered), ct);
                int totalBytes = bytesBuffered + bytesRead;
                bool isFinalBlock = bytesRead == 0;

                if (totalBytes == 0) break;

                var reader = new Utf8JsonReader(buffer.AsSpan(0, totalBytes), isFinalBlock, readerState);

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.String)
                    {
                        string? val = reader.GetString();
                        if (!string.IsNullOrEmpty(val))
                        {
                            WordCountHelper.AnalyzeText(val.AsSpan(), lookup, frequencies, _delimiters);
                        }
                    }
                }

                if (isFinalBlock) break;

                bytesBuffered = (int)(totalBytes - reader.BytesConsumed);
                readerState = reader.CurrentState;

                if (bytesBuffered > 0)
                {
                    // If buffer is full and we made no progress, we have a token larger than buffer
                    if (bytesBuffered == buffer.Length)
                    {
                        GrowBuffer(ref buffer);
                    }
                    else
                    {
                        // Move remaining bytes to the beginning
                        buffer.AsSpan((int)reader.BytesConsumed, bytesBuffered).CopyTo(buffer);
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Error parsing JSON. Path: {Path}, Line: {Line}, Position: {Position}", ex.Path, ex.LineNumber, ex.BytePositionInLine);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error in JSON counter");
            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return frequencies;
    }

    private static void GrowBuffer(ref byte[] buffer)
    {
        byte[] newBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
        buffer.AsSpan().CopyTo(newBuffer);
        ArrayPool<byte>.Shared.Return(buffer);
        buffer = newBuffer;
    }
}
