using Bunit;
using LocalFinanceManager.Components.Shared;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace LocalFinanceManager.Tests.Components;

[TestFixture]
public class AssignmentAuditTrailTests
{
    [TestCase("ValidationFailed")]
    [TestCase("ValidationError")]
    public void AuditTrail_ValidationActions_ShouldUseSameMappedVisuals(string actionType)
    {
        using var context = new BunitContext();

        var transactionId = Guid.NewGuid();
        var assignmentServiceMock = new Mock<ITransactionAssignmentService>();
        assignmentServiceMock
            .Setup(x => x.GetTransactionAuditHistoryAsync(transactionId))
            .ReturnsAsync(new List<TransactionAuditDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    TransactionId = transactionId,
                    ActionType = actionType,
                    ChangedBy = "System",
                    ChangedAt = DateTime.UtcNow,
                    AfterState = "{\"ErrorCode\":\"SplitValidationFailed\"}"
                }
            });

        context.Services.AddSingleton(assignmentServiceMock.Object);
        context.Services.AddLogging();

        var cut = context.Render<AssignmentAuditTrail>(parameters => parameters
            .Add(p => p.TransactionId, transactionId)
            .Add(p => p.IsVisible, true));

        cut.WaitForAssertion(() =>
        {
            var headingText = cut.Find("h6.card-subtitle").TextContent;
            Assert.That(headingText, Does.Contain("Validatie mislukt"));

            var iconClass = cut.Find("h6.card-subtitle i").GetAttribute("class");
            Assert.That(iconClass, Does.Contain("bi-exclamation-triangle"));

            var badgeClass = cut.Find("span.badge").GetAttribute("class");
            Assert.That(badgeClass, Does.Contain("bg-danger"));
        });
    }
}