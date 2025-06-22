using System;

namespace DevOps_GPT41;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}

public class DefaultDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}