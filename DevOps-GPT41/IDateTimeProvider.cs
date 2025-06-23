using System.Text.Json;

namespace DevOps_GPT41;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
