using LogCather3000Library;
using Microsoft.Extensions.Logging;
using NUnit.Framework.Internal;
using Moq;
using Castle.Core.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

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
                { "Logging:EnableResponseLogging", "false" }
            };
            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            RequestDelegate next = (HttpContext ctx) => Task.CompletedTask;

            _middleware = new RequestLoggingMiddleware(next, _mockLogger.Object, _config);
        }

        [Test]
        public async Task Middleware_Should_Not_Log_Request()
        {
            var config = BuildConfiguration(enableRequest: false, enableResponse: false);
            var middleware = new RequestLoggingMiddleware(
                next: (ctx) => Task.CompletedTask,
                logger:  _mockLogger.Object,
                config: config);

            var context = new DefaultHttpContext();
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("Test"));

            await middleware.Invoke(context);

            _mockLogger.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never
                );
        }

        [Test]
        public async Task Middleware_Should_Log_Request1()
        {
            var config = BuildConfiguration(enableRequest: false, enableResponse: false);
            var middleware = new RequestLoggingMiddleware(
                next: (ctx) => Task.CompletedTask,
                logger: _mockLogger.Object,
                config: config);

            var context = new DefaultHttpContext();
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("Test"));
            context.Request.Headers["X-Test-Header"] = "header-value";

            await middleware.Invoke(context);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString().Contains("test body") &&
                        v.ToString().Contains("X-Test-Header")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once
            );
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
        It.Is<It.IsAnyType>((state, type) => ValidateJsonLog(state.ToString())),
        It.IsAny<Exception>(),
        It.IsAny<Func<It.IsAnyType, Exception, string>>()),
    Times.Once);
        }


        private bool ValidateJsonLog(string logMessage)
        {
            try
            {
                using var doc = JsonDocument.Parse(logMessage);
                var root = doc.RootElement;

                var methodCorrect = root.TryGetProperty("Method", out var method) &&
                                    method.GetString() == "POST";

                var pathCorrect = root.TryGetProperty("Path", out var path) &&
                                  path.TryGetProperty("Value", out var pathValue) &&
                                  pathValue.GetString() == "/test";

                var bodyCorrect = root.TryGetProperty("Body", out var body) &&
                                  body.GetString().Contains("\"name\": \"Test\"");

                return methodCorrect && pathCorrect && bodyCorrect;
            }
            catch
            {
                return false;
            }
        }

        private Microsoft.Extensions.Configuration.IConfiguration BuildConfiguration(bool enableRequest, bool enableResponse)
        {
            var configValues = new Dictionary<string, string>
            {
                { "Logging:EnableRequestLogging", enableRequest.ToString() },
                { "Logging:EnableResponseLogging", enableResponse.ToString() }
            };
            return new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();
        }

    }
}