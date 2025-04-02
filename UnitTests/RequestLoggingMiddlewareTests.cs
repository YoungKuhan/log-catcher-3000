using LogCather3000Library;
using Microsoft.Extensions.Logging;
using NUnit.Framework.Internal;
using Moq;
using Castle.Core.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Net.Mime;
using System.Reflection.PortableExecutable;
using System.Web;

namespace LogCatcher3000.Tests
{
    public class RequestLoggingMiddlewareTests
    {
        private Mock<ILogger<RequestLoggingMiddleware>> _mockLogger;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<RequestLoggingMiddleware>>();
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
        public async Task Middleware_Should_Log_Request_And_Should_Not_Log_Response()
        {
            var config = BuildConfiguration(enableRequest: true, enableResponse: false);
            var middleware = new RequestLoggingMiddleware(
                next: (ctx) => Task.CompletedTask,
                logger: _mockLogger.Object,
                config: config);

            var context = BuildContext();

            await middleware.Invoke(context);

            _mockLogger.Verify(
                    x => x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) =>
                            v.ToString().Contains("\"Host\":\"test\"") &&             
                            v.ToString().Contains("\"ContentType\":\"application/json\"") &&
                            v.ToString().Contains("\"Method\":\"POST\"") &&
                            v.ToString().Contains("\"Path\":\"/test/test\"") &&      
                            //v.ToString().Contains("\"QueryString\":\"?test&test\"") && 
                            v.ToString().Contains("\"X-Test-Header\":[\"header-value\"]") &&
                            v.ToString().Contains("\"Body\":\"Test\"")
                        ),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                    Times.Once
                );

            _mockLogger.Verify(
                x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString().Contains("Outgoing Response")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never
            );
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

        private DefaultHttpContext BuildContext()
        {
            var context = new DefaultHttpContext();
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("Test"));
            context.Request.Headers["X-Test-Header"] = "header-value";
            context.Request.Method = "POST";
            context.Request.Host = new HostString("test");
            context.Request.ContentType = "application/json";
            context.Request.Path = "/test/test";

            var url = "https://www.microsoft.com?name=John&age=30&location=USA";
            var parsedUrl = url.Split('?')[1];

            // The ParseQueryString method will parse the query string and return a NameValueCollection
            var paramsCollection = HttpUtility.ParseQueryString(parsedUrl);
            //context.Request.QueryString = new QueryString("?test&test");

            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            context.Response.Body = new MemoryStream(Encoding.UTF8.GetBytes("Test"));

            return context;
        }

    }
}