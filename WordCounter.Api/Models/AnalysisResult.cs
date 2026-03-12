namespace WordCounter.Api.Models;

public record AnalysisResult(
    string Url,
    int WordCount,
    IEnumerable<KeyValuePair<string, int>> WordFrequencies,
    string Status,
    TimeSpan Duration
);
