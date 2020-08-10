using System;
using System.Collections.Generic;
using Xunit;

namespace Converter.Model.Tests
{
    public class ComponentTests
    {
        [Theory]
        [MemberData(nameof(ValidRestEndpointData))]
        public void ComponentCtor_ForRestEndpoint_WithValidName_ShouldParseNameCorrectly(string httpMethod,
            string endpoint)
        {
            // Arrange
            var endpointName = $"{httpMethod} {endpoint}";

            // Act
            var restEndpoint = new Component(endpointName, ComponentType.RestEndpoint);

            // Assert
            var actualHttpMethod = Assert.Contains("HttpMethod", restEndpoint.Properties);
            Assert.Equal(httpMethod, actualHttpMethod);
            var actualEndpoint = Assert.Contains("Endpoint", restEndpoint.Properties);
            Assert.Equal(endpoint, actualEndpoint);
        }

        public static IEnumerable<object[]> ValidRestEndpointData
        {
            get
            {
                var validMethods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH" };
                var validEndpoints = new[] { "/", "/a", "/{a}", "/a/b/c", "/a/{b}", "/a/{b}/{c}" };
                var result = new List<object[]>();
                foreach (var method in validMethods)
                    foreach (var endpoint in validEndpoints)
                        result.Add(new object[] { method, endpoint });
                return result;
            }
        }


        [Theory]
        [InlineData("GET/")]
        [InlineData("GET abc")]
        [InlineData("made-up /abc")]
        public void ComponentCtor_ForRestEndpoint_WithInvalidName_ShouldThrow(string endpointName)
        {
            // Arrange
            void Action()
            {
                _ = new Component(endpointName, ComponentType.RestEndpoint);
            }

            // Act

            // Assert
            Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void ComponentCtor_ForFunction_WithoutRuntime_ShouldFallbackToDefaultRuntime()
        {
            // Arrange

            // Act
            var function = new Component("functionName", ComponentType.Function);

            // Assert
            var actualRuntime = Assert.Contains("Runtime", function.Properties);
            Assert.Equal("default", actualRuntime);
        }

        [Theory]
        [InlineData("dotnetcore2.1")]
        [InlineData("dotnetcore3.1")]
        [InlineData("nodejs12.x")]
        public void ComponentCtor_ForFunction_WithValidRuntime_ShouldSucceed(string runtime)
        {
            // Arrange

            // Act
            var function = new Component(
                "functionName",
                ComponentType.Function,
                new Dictionary<string, string> {
                    { "Runtime", runtime }
                }
            );

            // Assert
            var actualRuntime = Assert.Contains("Runtime", function.Properties);
            Assert.Equal(runtime, actualRuntime);
        }
    }
}