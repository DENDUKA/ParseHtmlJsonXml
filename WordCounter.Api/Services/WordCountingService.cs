using System.Diagnostics;
using Microsoft.Extensions.Logging;
using WordCounter.Api.Interfaces;
using WordCounter.Api.Models;
using WordCounter.Domain.Enums;
using WordCounter.Domain.Interfaces;
using WordCounter.Domain.Models;

namespace WordCounter.Api.Services;

public class WordCountingService(
    IContentProvider contentProvider,
    IWordCounterFactory counterFactory,
    ILogger<WordCountingService> logger,
    WordCounterSettings settings) : IWordCountingService
{
    private readonly SemaphoreSlim _semaphore = new(settings.MaxConcurrency);

    public async Task<IEnumerable<AnalysisResult>> Analyze(IEnumerable<string> urls, ContentType type, CancellationToken ct)
    {
        var tasks = urls.Distinct().Select(url => ProcessUrl(url, type, ct));
        return await Task.WhenAll(tasks);
    }

    private async Task<AnalysisResult> ProcessUrl(string url, ContentType type, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        await _semaphore.WaitAsync(ct);

        try
        {
            logger.LogInformation("Starting analysis for URL: {Url}, Type: {Type}", url, type);

            using var stream = await contentProvider.GetContentStream(url, ct);
            var counter = counterFactory.CreateCounter(type);
            var frequencies = await counter.CountWords(stream, ct);

            var sortedFrequencies = frequencies
                .OrderByDescending(x => x.Value)
                .ToList();

            int wordCount = frequencies.Sum(x => x.Value);

            sw.Stop();
            logger.LogInformation("Completed analysis for URL: {Url}. Words: {Count}. Duration: {Duration}ms", url, wordCount, sw.ElapsedMilliseconds);

            return new AnalysisResult(url, wordCount, sortedFrequencies, "Success", sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Error analyzing URL: {Url}", url);
            return new AnalysisResult(url, 0, [], $"Error: {ex.Message}", sw.Elapsed);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
