namespace WordCounter.Domain.Models;

public class WordCounterSettings
{
    private const int DefaultMaxConcurrency = 10;
    private const int DefaultTimeoutSeconds = 15;
    
    public string[] Delimiters { get; set; } = [];
    public int MaxConcurrency { get; set; } = DefaultMaxConcurrency;
    public int TimeoutSeconds { get; set; } = DefaultTimeoutSeconds;
}
