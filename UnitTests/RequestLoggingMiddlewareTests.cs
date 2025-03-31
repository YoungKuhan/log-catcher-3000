using LogCather3000Library;
using Microsoft.Extensions.Logging;
using NUnit.Framework.Internal;
using Moq;
using Castle.Core.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace LogCatcher3000.Tests
{
    public class RequestLoggingMiddlewareTests
    {
        private Mock<ILogger<RequestLoggingMiddleware>> _mockLogger;
        private Microsoft.Extensions.Configuration.IConfiguration _config;
        private RequestLoggingMiddleware _middleware;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<RequestLoggingMiddleware>>();

            var configValues = new Dictionary<string, string>
            {
                { "Logging:EnableRequestLogging", "true" },
                { "Logging:EnableResponseLogging", "true" },
                { "Logging:MaxLogBodySize", "1024" }
            };
            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            RequestDelegate next = (HttpContext ctx) => Task.CompletedTask;

            _middleware = new RequestLoggingMiddleware(next, _mockLogger.Object, _config);
        }

        [Test]
        public async Task Middleware_Should_Log_Request()
        {
            var context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.Path = "/test";
            context.Request.ContentType = "application/json";   
            var requestBody = "{\"name\": \"Test\"}";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
            context.Request.Body.Seek(0, SeekOrigin.Begin);

            await _middleware.Invoke(context);

            _mockLogger.Verify(logger =>
                logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("\"Method\":\"POST\"") &&
                                                          state.ToString().Contains("\"Path\":\"/test\"")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}