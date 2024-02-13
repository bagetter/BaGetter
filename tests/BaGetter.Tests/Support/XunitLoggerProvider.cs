using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace BaGetter.Tests;

/// <remarks>
/// Based off: <see href="https://stackoverflow.com/questions/46169169/net-core-2-0-configurelogging-xunit-test"/>
/// </remarks>
public class XunitLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _output;

    public XunitLoggerProvider(ITestOutputHelper output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    public ILogger CreateLogger(string category) => new XunitLogger(_output, category);

    public void Dispose() { }
}
