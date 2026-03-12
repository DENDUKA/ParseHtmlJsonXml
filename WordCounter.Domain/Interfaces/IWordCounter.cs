namespace WordCounter.Domain.Interfaces;

public interface IWordCounter
{
    Task<Dictionary<string, int>> CountWords(Stream stream, CancellationToken ct);
}
