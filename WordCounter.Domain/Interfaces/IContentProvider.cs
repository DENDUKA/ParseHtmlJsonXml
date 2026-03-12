namespace WordCounter.Domain.Interfaces;

public interface IContentProvider
{
    Task<Stream> GetContentStream(string url, CancellationToken ct);
}
