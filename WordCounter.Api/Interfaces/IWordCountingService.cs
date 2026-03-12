using WordCounter.Api.Models;
using WordCounter.Domain.Enums;

namespace WordCounter.Api.Interfaces;

public interface IWordCountingService
{
    Task<IEnumerable<AnalysisResult>> Analyze(IEnumerable<string> urls, ContentType type, CancellationToken ct);
}
