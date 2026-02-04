// -----------------------------------------------------------------------
// <copyright file="SseParserTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;
using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.LLM;

/// <summary>
/// Unit tests for <see cref="SseParser"/> class.
/// </summary>
public class SseParserTests
{
    /// <summary>
    /// Tests that ParseStreamAsync extracts data from SSE lines.
    /// </summary>
    [Fact]
    public async Task ParseStreamAsync_WithValidSseData_ShouldExtractPayloads()
    {
        // Arrange
        var sseContent = """
            data: {"token": "Hello"}
            data: {"token": " world"}
            data: [DONE]
            """;
        using var stream = CreateStream(sseContent);

        // Act
        var payloads = new List<string>();
        await foreach (var payload in SseParser.ParseStreamAsync(stream))
        {
            payloads.Add(payload);
        }

        // Assert
        payloads.Should().HaveCount(2);
        payloads[0].Should().Be("{\"token\": \"Hello\"}");
        payloads[1].Should().Be("{\"token\": \" world\"}");
    }

    /// <summary>
    /// Tests that ParseStreamAsync stops at [DONE] marker.
    /// </summary>
    [Fact]
    public async Task ParseStreamAsync_WithDoneMarker_ShouldStopParsing()
    {
        // Arrange
        var sseContent = """
            data: first
            data: [DONE]
            data: should-not-appear
            """;
        using var stream = CreateStream(sseContent);

        // Act
        var payloads = new List<string>();
        await foreach (var payload in SseParser.ParseStreamAsync(stream))
        {
            payloads.Add(payload);
        }

        // Assert
        payloads.Should().HaveCount(1);
        payloads[0].Should().Be("first");
    }

    /// <summary>
    /// Tests that ParseStreamAsync ignores empty lines.
    /// </summary>
    [Fact]
    public async Task ParseStreamAsync_WithEmptyLines_ShouldIgnoreThem()
    {
        // Arrange
        var sseContent = """
            data: first

            data: second

            data: [DONE]
            """;
        using var stream = CreateStream(sseContent);

        // Act
        var payloads = new List<string>();
        await foreach (var payload in SseParser.ParseStreamAsync(stream))
        {
            payloads.Add(payload);
        }

        // Assert
        payloads.Should().HaveCount(2);
    }

    /// <summary>
    /// Tests that ParseStreamAsync ignores non-data lines.
    /// </summary>
    [Fact]
    public async Task ParseStreamAsync_WithNonDataLines_ShouldIgnoreThem()
    {
        // Arrange
        var sseContent = """
            event: message
            id: 123
            retry: 1000
            data: payload
            data: [DONE]
            """;
        using var stream = CreateStream(sseContent);

        // Act
        var payloads = new List<string>();
        await foreach (var payload in SseParser.ParseStreamAsync(stream))
        {
            payloads.Add(payload);
        }

        // Assert
        payloads.Should().HaveCount(1);
        payloads[0].Should().Be("payload");
    }

    /// <summary>
    /// Tests that ParseStreamAsync throws on null stream.
    /// </summary>
    [Fact]
    public async Task ParseStreamAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act
        var action = async () =>
        {
            await foreach (var _ in SseParser.ParseStreamAsync(null!))
            {
                // Should throw before we get here
            }
        };

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that ParseStreamAsync respects cancellation.
    /// </summary>
    [Fact]
    public async Task ParseStreamAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var sseContent = """
            data: first
            data: second
            data: third
            data: [DONE]
            """;
        using var stream = CreateStream(sseContent);
        using var cts = new CancellationTokenSource();

        // Act
        var payloads = new List<string>();
        var action = async () =>
        {
            await foreach (var payload in SseParser.ParseStreamAsync(stream, cts.Token))
            {
                payloads.Add(payload);
                if (payloads.Count == 1)
                {
                    cts.Cancel();
                }
            }
        };

        // Assert
        await action.Should().ThrowAsync<OperationCanceledException>();
        payloads.Should().HaveCount(1);
    }

    /// <summary>
    /// Tests IsEndOfStream with various inputs.
    /// </summary>
    [Theory]
    [InlineData("data: [DONE]", true)]
    [InlineData("data: payload", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("event: done", false)]
    public void IsEndOfStream_ShouldReturnCorrectValue(string? line, bool expected)
    {
        // Act
        var result = SseParser.IsEndOfStream(line);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Tests ExtractData with various inputs.
    /// </summary>
    [Theory]
    [InlineData("data: payload", "payload")]
    [InlineData("data: {\"key\": \"value\"}", "{\"key\": \"value\"}")]
    [InlineData("event: done", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void ExtractData_ShouldReturnCorrectValue(string? line, string? expected)
    {
        // Act
        var result = SseParser.ExtractData(line);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Tests constants have expected values.
    /// </summary>
    [Fact]
    public void Constants_ShouldHaveExpectedValues()
    {
        // Assert
        SseParser.DataPrefix.Should().Be("data: ");
        SseParser.DoneMarker.Should().Be("[DONE]");
    }

    /// <summary>
    /// Creates a stream from a string.
    /// </summary>
    private static MemoryStream CreateStream(string content)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(content));
    }
}
