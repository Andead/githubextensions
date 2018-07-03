using System;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tools.Github.Extensions.Tests
{
    public static class MockExtensions
    {
        public static void VerifyLogged(this Mock<ILogger> mock, LogLevel? logLevel = null, Times? times = null)
        {
            mock.Verify(l => l.Log<object>(logLevel ?? It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<object>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<object, Exception, string>>()),
                times ?? Times.AtLeast(1));
        }
    }
}