using LocalFinanceManager.Controllers;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.Services;
using LocalFinanceManager.Data.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Moq;

namespace LocalFinanceManager.Tests.Controllers;

[TestFixture]
public class TransactionsControllerAuditTests
{
    private Mock<ITransactionRepository> _repositoryMock = null!;
    private Mock<ITransactionAssignmentService> _assignmentServiceMock = null!;
    private Mock<ILogger<TransactionsController>> _loggerMock = null!;
    private TransactionsController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _repositoryMock = new Mock<ITransactionRepository>();
        _assignmentServiceMock = new Mock<ITransactionAssignmentService>();
        _loggerMock = new Mock<ILogger<TransactionsController>>();

        _controller = new TransactionsController(
            _repositoryMock.Object,
            null!,  // ImportService not used by GetAuditHistory
            _assignmentServiceMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task GetAuditHistory_ReturnsAuditHistory()
    {
        var transactionId = Guid.NewGuid();
        var expectedAudits = new List<TransactionAuditDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TransactionId = transactionId,
                ActionType = "Assign",
                ChangedBy = "System",
                ChangedAt = DateTime.UtcNow.AddHours(-1),
                IsAutoApplied = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                TransactionId = transactionId,
                ActionType = "AutoAssign",
                ChangedBy = "AutoApplyService",
                ChangedAt = DateTime.UtcNow.AddMinutes(-30),
                IsAutoApplied = true,
                Confidence = 0.85f,
                ModelVersion = 1
            }
        };

        _assignmentServiceMock
            .Setup(s => s.GetTransactionAuditHistoryAsync(transactionId))
            .ReturnsAsync(expectedAudits);

        var result = await _controller.GetAuditHistory(transactionId);

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var audits = okResult!.Value as List<TransactionAuditDto>;
        Assert.That(audits, Is.Not.Null);
        Assert.That(audits!.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetAuditHistory_ReturnsAuditHistoryFromService()
    {
        var transactionId = Guid.NewGuid();
        var older = DateTime.UtcNow.AddDays(-2);
        var newer = DateTime.UtcNow.AddDays(-1);

        var audits = new List<TransactionAuditDto>
        {
            new() { Id = Guid.NewGuid(), TransactionId = transactionId, ActionType = "Assign", ChangedBy = "System", ChangedAt = older },
            new() { Id = Guid.NewGuid(), TransactionId = transactionId, ActionType = "Reassign", ChangedBy = "System", ChangedAt = newer }
        };

        _assignmentServiceMock
            .Setup(s => s.GetTransactionAuditHistoryAsync(transactionId))
            .ReturnsAsync(audits);

        var result = await _controller.GetAuditHistory(transactionId);

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var returnedAudits = okResult!.Value as List<TransactionAuditDto>;
        Assert.That(returnedAudits, Is.Not.Null);
        // Service is responsible for ordering; controller just passes through
        Assert.That(returnedAudits!.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetAuditHistory_IncludesAutoAppliedFlag()
    {
        var transactionId = Guid.NewGuid();
        var audits = new List<TransactionAuditDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TransactionId = transactionId,
                ActionType = "AutoAssign",
                ChangedBy = "AutoApplyService",
                ChangedAt = DateTime.UtcNow,
                IsAutoApplied = true,
                Confidence = 0.92f,
                ModelVersion = 2
            }
        };

        _assignmentServiceMock
            .Setup(s => s.GetTransactionAuditHistoryAsync(transactionId))
            .ReturnsAsync(audits);

        var result = await _controller.GetAuditHistory(transactionId);

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var returnedAudits = okResult!.Value as List<TransactionAuditDto>;
        Assert.That(returnedAudits, Is.Not.Null);
        var entry = returnedAudits![0];
        Assert.That(entry.IsAutoApplied, Is.True);
        Assert.That(entry.Confidence, Is.EqualTo(0.92f).Within(0.001f));
        Assert.That(entry.ModelVersion, Is.EqualTo(2));
    }

    [Test]
    public async Task GetAuditHistory_EmptyResult_ReturnsNotFoundProblemDetails()
    {
        var transactionId = Guid.NewGuid();

        _assignmentServiceMock
            .Setup(s => s.GetTransactionAuditHistoryAsync(transactionId))
            .ReturnsAsync(new List<TransactionAuditDto>());

        var result = await _controller.GetAuditHistory(transactionId);

        var notFoundResult = result.Result as NotFoundObjectResult;
        Assert.That(notFoundResult, Is.Not.Null);
        var problemDetails = notFoundResult!.Value as ProblemDetails;
        Assert.That(problemDetails, Is.Not.Null);
        Assert.That(problemDetails!.Status, Is.EqualTo(404));
    }

    [Test]
    public async Task GetAuditHistory_ServiceThrows_ReturnsNotFoundProblemDetails()
    {
        var transactionId = Guid.NewGuid();
        const string errorMessage = "Transaction not found or not accessible.";

        _assignmentServiceMock
            .Setup(s => s.GetTransactionAuditHistoryAsync(transactionId))
            .ThrowsAsync(new InvalidOperationException(errorMessage));

        var result = await _controller.GetAuditHistory(transactionId);

        var objectResult = result.Result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);
        Assert.That(objectResult!.StatusCode, Is.EqualTo(404));
        Assert.That(objectResult.Value, Is.TypeOf<ProblemDetails>());

        var problemDetails = objectResult.Value as ProblemDetails;
        Assert.That(problemDetails, Is.Not.Null);
        Assert.That(problemDetails!.Detail, Is.EqualTo(errorMessage));
    }
}
