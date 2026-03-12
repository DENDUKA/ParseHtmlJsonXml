namespace WordCounter.Api.Models;

public record AnalysisResultDto(
    string Url,
    int WordCount,
    IEnumerable<KeyValuePair<string, int>> WordFrequencies,
    string Status,
    TimeSpan Duration
);
