﻿using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Threading.Tasks;
using Wiz.Template.API.Middlewares;
using Wiz.Template.API.Settings;
using Xunit;

namespace Wiz.Template.Tests.Unit.Middlewares
{
    public class ErrorHandlerMiddlewareUnitTest
    {
        private readonly Mock<IOptions<ApplicationInsightsSettings>> _applicationInsightsMock;
        private readonly Mock<IHostingEnvironment> _hostingEnvironmentMock;

        public ErrorHandlerMiddlewareUnitTest()
        {
            _applicationInsightsMock = new Mock<IOptions<ApplicationInsightsSettings>>();
            _hostingEnvironmentMock = new Mock<IHostingEnvironment>();
        }

        [Fact]
        public async Task InvokeErrorHandler_ExceptionTestAsync()
        {
            var applicationInsightsMock = new ApplicationInsightsSettings("118047f1-b165-4bff-9471-e87fd3fe167c");

            _applicationInsightsMock.Setup(x => x.Value)
                .Returns(applicationInsightsMock);

            _hostingEnvironmentMock.Setup(x => x.EnvironmentName)
                .Returns("Development");

            var httpContext = new DefaultHttpContext().Request.HttpContext;
            var exceptionHandlerFeature = new ExceptionHandlerFeature()
            {
                Error = new Exception("Mock error exception")
            };

            httpContext.Features.Set<IExceptionHandlerFeature>(exceptionHandlerFeature);

            var errorHandlerMiddleware = new ErrorHandlerMiddleware(_applicationInsightsMock.Object, _hostingEnvironmentMock.Object);
            await errorHandlerMiddleware.Invoke(httpContext);

            Assert.NotNull(errorHandlerMiddleware);
        }

        [Fact]
        public async Task InvokeErrorHandler_NotExceptionTestAsync()
        {
            var applicationInsightsMock = new ApplicationInsightsSettings("118047f1-b165-4bff-9471-e87fd3fe167c");

            _applicationInsightsMock.Setup(x => x.Value)
                .Returns(applicationInsightsMock);

            _hostingEnvironmentMock.Setup(x => x.EnvironmentName)
                .Returns("Development");

            var httpContext = new DefaultHttpContext().Request.HttpContext;

            var errorHandlerMiddleware = new ErrorHandlerMiddleware(_applicationInsightsMock.Object, _hostingEnvironmentMock.Object);
            await errorHandlerMiddleware.Invoke(httpContext);

            Assert.NotNull(errorHandlerMiddleware);
        }
    }
}
