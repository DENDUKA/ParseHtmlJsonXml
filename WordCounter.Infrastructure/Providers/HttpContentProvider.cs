using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WordCounter.Domain.Interfaces;

namespace WordCounter.Infrastructure.Providers
{
    public class HttpContentProvider : IContentProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HttpContentProvider(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<Stream> GetContentStream(string url, CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient();
            return await client.GetStreamAsync(url, ct);
        }
    }
}
