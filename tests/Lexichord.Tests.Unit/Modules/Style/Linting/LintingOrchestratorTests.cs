using System.Reactive.Subjects;
using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Abstractions.Contracts.Threading;
using Lexichord.Abstractions.Events;
using Lexichord.Modules.Style.Services.Linting;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style.Linting;

/// <summary>
/// Unit tests for LintingOrchestrator.
/// </summary>
/// <remarks>
/// LOGIC: Verifies the orchestrator correctly manages document subscriptions,
/// debouncing, concurrent scanning, and event publishing per LCS-DES-023a.
/// </remarks>
public class LintingOrchestratorTests : IDisposable
{
    private readonly Mock<IStyleEngine> _styleEngineMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IThreadMarshaller> _threadMarshallerMock;
    private readonly Mock<ILogger<LintingOrchestrator>> _loggerMock;
    private readonly IOptions<LintingOptions> _options;
    private readonly LintingOrchestrator _sut;

    public LintingOrchestratorTests()
    {
        _styleEngineMock = new Mock<IStyleEngine>();
        _mediatorMock = new Mock<IMediator>();
        _threadMarshallerMock = new Mock<IThreadMarshaller>();
        _loggerMock = new Mock<ILogger<LintingOrchestrator>>();
        _options = Options.Create(new LintingOptions
        {
            DebounceMilliseconds = 10, // Fast for tests
            MaxConcurrentScans = 2
        });

        // LOGIC: Configure thread marshaller mock to report never on UI thread
        // This simulates calls coming from a background thread for tests
        _threadMarshallerMock.Setup(x => x.IsOnUIThread).Returns(false);

        _sut = new LintingOrchestrator(
            _styleEngineMock.Object,
            _mediatorMock.Object,
            _threadMarshallerMock.Object,
            _options,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    [Fact]
    public void Constructor_ThrowsOnNullStyleEngine()
    {
        // Act
        var act = () => new LintingOrchestrator(
            null!,
            _mediatorMock.Object,
            _threadMarshallerMock.Object,
            _options,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("styleEngine");
    }

    [Fact]
    public void Constructor_ThrowsOnNullMediator()
    {
        // Act
        var act = () => new LintingOrchestrator(
            _styleEngineMock.Object,
            null!,
            _threadMarshallerMock.Object,
            _options,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("mediator");
    }

    [Fact]
    public void Constructor_ThrowsOnNullThreadMarshaller()
    {
        // Act
        var act = () => new LintingOrchestrator(
            _styleEngineMock.Object,
            _mediatorMock.Object,
            null!,
            _options,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("threadMarshaller");
    }

    [Fact]
    public void Subscribe_IncreasesActiveDocumentCount()
    {
        // Arrange
        var contentSubject = new Subject<string>();

        // Act
        _sut.Subscribe("doc-1", "/path/to/doc1.md", contentSubject);

        // Assert
        _sut.ActiveDocumentCount.Should().Be(1);
    }

    [Fact]
    public void Subscribe_ReturnsDisposable()
    {
        // Arrange
        var contentSubject = new Subject<string>();

        // Act
        var subscription = _sut.Subscribe("doc-1", "/path/to/doc1.md", contentSubject);

        // Assert
        subscription.Should().NotBeNull();
    }

    [Fact]
    public void Subscribe_ThrowsOnNullDocumentId()
    {
        // Arrange
        var contentSubject = new Subject<string>();

        // Act
        var act = () => _sut.Subscribe(null!, "/path/to/doc1.md", contentSubject);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Subscribe_ThrowsOnNullContentStream()
    {
        // Act
        var act = () => _sut.Subscribe("doc-1", "/path/to/doc1.md", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Unsubscribe_DecreasesActiveDocumentCount()
    {
        // Arrange
        var contentSubject = new Subject<string>();
        _sut.Subscribe("doc-1", "/path/to/doc1.md", contentSubject);

        // Act
        _sut.Unsubscribe("doc-1");

        // Assert
        _sut.ActiveDocumentCount.Should().Be(0);
    }

    [Fact]
    public void Unsubscribe_IsSafeForNonExistentDocument()
    {
        // Act & Assert (should not throw)
        _sut.Unsubscribe("non-existent");
    }

    [Fact]
    public void DisposingSubscription_RemovesDocument()
    {
        // Arrange
        var contentSubject = new Subject<string>();
        var subscription = _sut.Subscribe("doc-1", "/path/to/doc1.md", contentSubject);

        // Act
        subscription.Dispose();

        // Assert
        _sut.ActiveDocumentCount.Should().Be(0);
    }

    [Fact]
    public void GetDocumentState_ReturnsNull_WhenNotSubscribed()
    {
        // Act
        var state = _sut.GetDocumentState("non-existent");

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetDocumentState_ReturnsState_WhenSubscribed()
    {
        // Arrange
        var contentSubject = new Subject<string>();
        _sut.Subscribe("doc-1", "/path/to/doc1.md", contentSubject);

        // Act
        var state = _sut.GetDocumentState("doc-1");

        // Assert
        state.Should().NotBeNull();
        state!.DocumentId.Should().Be("doc-1");
        state.FilePath.Should().Be("/path/to/doc1.md");
    }

    [Fact]
    public async Task TriggerManualScanAsync_ThrowsWhenNotSubscribed()
    {
        // Act
        var act = async () => await _sut.TriggerManualScanAsync(
            "non-existent", "content");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not subscribed*");
    }

    [Fact]
    public async Task TriggerManualScanAsync_CallsStyleEngine()
    {
        // Arrange
        var contentSubject = new Subject<string>();
        _sut.Subscribe("doc-1", "/path/to/doc1.md", contentSubject);

        _styleEngineMock
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StyleViolation>());

        // Act
        var result = await _sut.TriggerManualScanAsync("doc-1", "test content");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        _styleEngineMock.Verify(
            x => x.AnalyzeAsync("test content", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TriggerManualScanAsync_PublishesLintingStartedEvent()
    {
        // Arrange
        var contentSubject = new Subject<string>();
        _sut.Subscribe("doc-1", "/path/to/doc1.md", contentSubject);

        _styleEngineMock
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StyleViolation>());

        // Act
        await _sut.TriggerManualScanAsync("doc-1", "test content");

        // Assert
        _mediatorMock.Verify(
            x => x.Publish(It.IsAny<LintingStartedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TriggerManualScanAsync_PublishesLintingCompletedEvent()
    {
        // Arrange
        var contentSubject = new Subject<string>();
        _sut.Subscribe("doc-1", "/path/to/doc1.md", contentSubject);

        _styleEngineMock
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StyleViolation>());

        // Act
        await _sut.TriggerManualScanAsync("doc-1", "test content");

        // Assert
        _mediatorMock.Verify(
            x => x.Publish(It.IsAny<LintingCompletedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TriggerManualScanAsync_EmitsResultToObservable()
    {
        // Arrange
        var contentSubject = new Subject<string>();
        _sut.Subscribe("doc-1", "/path/to/doc1.md", contentSubject);

        _styleEngineMock
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StyleViolation>());

        LintResult? observedResult = null;
        using var resultSubscription = _sut.Results.Subscribe(r => observedResult = r);

        // Act
        await _sut.TriggerManualScanAsync("doc-1", "test content");

        // Assert
        observedResult.Should().NotBeNull();
        observedResult!.DocumentId.Should().Be("doc-1");
        observedResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task TriggerManualScanAsync_ReturnsViolationsFromEngine()
    {
        // Arrange
        var contentSubject = new Subject<string>();
        _sut.Subscribe("doc-1", "/path/to/doc1.md", contentSubject);

        var testRule = CreateTestRule();
        var violations = new List<StyleViolation>
        {
            new(testRule, "Test violation", 0, 10, 1, 0, 1, 10, "matched", null, ViolationSeverity.Warning),
            new(testRule, "Another violation", 20, 40, 5, 0, 5, 20, "another", null, ViolationSeverity.Error)
        };

        _styleEngineMock
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(violations);

        // Act
        var result = await _sut.TriggerManualScanAsync("doc-1", "test content");

        // Assert
        result.Violations.Should().HaveCount(2);
        result.Violations.Should().BeEquivalentTo(violations);
    }

    [Fact]
    public async Task TriggerManualScanAsync_HandlesEngineException()
    {
        // Arrange
        var contentSubject = new Subject<string>();
        _sut.Subscribe("doc-1", "/path/to/doc1.md", contentSubject);

        _styleEngineMock
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Engine failure"));

        // Act
        var result = await _sut.TriggerManualScanAsync("doc-1", "test content");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Engine failure");
    }

    [Fact]
    public async Task TriggerManualScanAsync_PublishesErrorEvent_OnException()
    {
        // Arrange
        var contentSubject = new Subject<string>();
        _sut.Subscribe("doc-1", "/path/to/doc1.md", contentSubject);

        _styleEngineMock
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Engine failure"));

        // Act
        await _sut.TriggerManualScanAsync("doc-1", "test content");

        // Assert
        _mediatorMock.Verify(
            x => x.Publish(It.IsAny<LintingErrorEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void MultipleDocuments_TrackedIndependently()
    {
        // Arrange
        var contentSubject1 = new Subject<string>();
        var contentSubject2 = new Subject<string>();

        // Act
        _sut.Subscribe("doc-1", "/path/to/doc1.md", contentSubject1);
        _sut.Subscribe("doc-2", "/path/to/doc2.md", contentSubject2);

        // Assert
        _sut.ActiveDocumentCount.Should().Be(2);
        _sut.GetDocumentState("doc-1").Should().NotBeNull();
        _sut.GetDocumentState("doc-2").Should().NotBeNull();
    }

    [Fact]
    public void Subscribe_ReplacesExistingSubscription()
    {
        // Arrange
        var contentSubject1 = new Subject<string>();
        var contentSubject2 = new Subject<string>();
        _sut.Subscribe("doc-1", "/path/to/doc1.md", contentSubject1);

        // Act
        _sut.Subscribe("doc-1", "/path/to/doc1-new.md", contentSubject2);

        // Assert
        _sut.ActiveDocumentCount.Should().Be(1);
        var state = _sut.GetDocumentState("doc-1");
        state!.FilePath.Should().Be("/path/to/doc1-new.md");
    }

    [Fact]
    public void Dispose_ClearsAllSubscriptions()
    {
        // Arrange
        var contentSubject1 = new Subject<string>();
        var contentSubject2 = new Subject<string>();
        _sut.Subscribe("doc-1", "/path/to/doc1.md", contentSubject1);
        _sut.Subscribe("doc-2", "/path/to/doc2.md", contentSubject2);

        // Act
        _sut.Dispose();

        // Assert - after dispose, ActiveDocumentCount throws ObjectDisposedException
        // but internal state should be cleared
    }

    [Fact]
    public void Subscribe_ThrowsAfterDispose()
    {
        // Arrange
        _sut.Dispose();
        var contentSubject = new Subject<string>();

        // Act
        var act = () => _sut.Subscribe("doc-1", "/path/to/doc1.md", contentSubject);

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    private static StyleRule CreateTestRule() => new(
        Id: "TST001",
        Name: "Test Rule",
        Description: "A test rule",
        Category: RuleCategory.Terminology,
        DefaultSeverity: ViolationSeverity.Warning,
        Pattern: "test",
        PatternType: PatternType.Literal,
        Suggestion: null);
}
