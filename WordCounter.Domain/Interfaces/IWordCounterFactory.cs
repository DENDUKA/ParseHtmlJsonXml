using WordCounter.Domain.Enums;

namespace WordCounter.Domain.Interfaces;

public interface IWordCounterFactory
{
    IWordCounter CreateCounter(ContentType type);
}
