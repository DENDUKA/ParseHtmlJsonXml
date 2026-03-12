using WordCounter.Domain.Enums;
using WordCounter.Domain.Interfaces;
using WordCounter.Infrastructure.Counters;
using Microsoft.Extensions.DependencyInjection;

namespace WordCounter.Infrastructure.Factories;

public class WordCounterFactory(IServiceProvider serviceProvider) : IWordCounterFactory
{
    public IWordCounter CreateCounter(ContentType type)
    {
        return type switch
        {
            ContentType.Html => serviceProvider.GetRequiredService<HtmlWordCounter>(),
            ContentType.Json => serviceProvider.GetRequiredService<JsonWordCounter>(),
            ContentType.Xml => serviceProvider.GetRequiredService<XmlWordCounter>(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
