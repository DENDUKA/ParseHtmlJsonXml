namespace WordCounter.Api.Models;

public class AnalysisRequest
{
    public required IEnumerable<string> Urls { get; set; }
}
