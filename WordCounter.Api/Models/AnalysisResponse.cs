namespace WordCounter.Api.Models;

public class AnalysisResponse
{
    public required IEnumerable<AnalysisResultDto> Results { get; set; }
}
