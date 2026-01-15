using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.Services;
using LocalFinanceManager.Services.Import;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Controllers;

/// <summary>
/// API controller for transaction operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionRepository _repository;
    private readonly ImportService _importService;
    private readonly ITransactionAssignmentService _assignmentService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        ITransactionRepository repository,
        ImportService importService,
        ITransactionAssignmentService assignmentService,
        ILogger<TransactionsController> logger)
    {
        _repository = repository;
        _importService = importService;
        _assignmentService = assignmentService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all transactions for a specific account.
    /// </summary>
    [HttpGet]
    [Route("account/{accountId}")]
    public async Task<ActionResult<List<TransactionDto>>> GetByAccountId(Guid accountId)
    {
        try
        {
            var transactions = await _repository.GetByAccountIdAsync(accountId);
            var dtos = transactions.Select(t => new TransactionDto
            {
                Id = t.Id,
                Amount = t.Amount,
                Date = t.Date,
                Description = t.Description,
                Counterparty = t.Counterparty,
                AccountId = t.AccountId,
                ExternalId = t.ExternalId,
                SourceFileName = t.SourceFileName,
                ImportedAt = t.ImportedAt,
                RowVersion = t.RowVersion
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transactions for account {AccountId}", accountId);
            return StatusCode(500, new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "Internal Server Error",
                status = 500
            });
        }
    }

    /// <summary>
    /// Previews an import file with suggested column mappings.
    /// </summary>
    [HttpPost]
    [Route("preview")]
    public async Task<ActionResult<ImportPreviewDto>> PreviewImport([FromBody] PreviewImportRequest request)
    {
        try
        {
            // Validate file format
            if (string.IsNullOrEmpty(request.FileFormat) || 
                !new[] { "csv", "json" }.Contains(request.FileFormat.ToLowerInvariant()))
            {
                return BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Invalid file format",
                    status = 400,
                    errors = new { FileFormat = new[] { "File format must be 'csv' or 'json'" } }
                });
            }

            // Decode Base64 file content
            byte[] fileBytes;
            try
            {
                fileBytes = Convert.FromBase64String(request.FileContent);
            }
            catch (FormatException)
            {
                return BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Invalid file content",
                    status = 400,
                    errors = new { FileContent = new[] { "File content must be Base64 encoded" } }
                });
            }

            var fileContent = System.Text.Encoding.UTF8.GetString(fileBytes);
            var preview = await _importService.PreviewImportAsync(fileContent, request.FileFormat);

            return Ok(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing import");
            return StatusCode(500, new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "Internal Server Error",
                status = 500,
                detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Imports transactions from CSV/JSON file.
    /// </summary>
    [HttpPost]
    [Route("import")]
    public async Task<ActionResult<ImportResultDto>> ImportTransactions([FromBody] ImportTransactionsRequest request)
    {
        try
        {
            // Validate request
            if (request.AccountId == Guid.Empty)
            {
                return BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Validation error",
                    status = 400,
                    errors = new { AccountId = new[] { "Account ID is required" } }
                });
            }

            if (string.IsNullOrEmpty(request.FileFormat) || 
                !new[] { "csv", "json" }.Contains(request.FileFormat.ToLowerInvariant()))
            {
                return BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Validation error",
                    status = 400,
                    errors = new { FileFormat = new[] { "File format must be 'csv' or 'json'" } }
                });
            }

            if (string.IsNullOrEmpty(request.FileContent))
            {
                return BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Validation error",
                    status = 400,
                    errors = new { FileContent = new[] { "File content is required" } }
                });
            }

            // Validate column mapping
            if (request.ColumnMapping == null || 
                string.IsNullOrEmpty(request.ColumnMapping.DateColumn) ||
                string.IsNullOrEmpty(request.ColumnMapping.AmountColumn))
            {
                return BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Validation error",
                    status = 400,
                    errors = new { ColumnMapping = new[] { "Date and Amount column mappings are required" } }
                });
            }

            // Import transactions
            var result = await _importService.ImportTransactionsAsync(request, request.AccountId);

            if (!result.Success && result.Errors.Any())
            {
                return BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Import failed",
                    status = 400,
                    detail = result.Message,
                    errors = result.Errors.GroupBy(e => e.FieldName)
                        .ToDictionary(g => g.Key, g => g.Select(e => $"Line {e.LineNumber}: {e.ErrorMessage}").ToArray())
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing transactions");
            return StatusCode(500, new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "Internal Server Error",
                status = 500,
                detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Assigns a transaction to a budget line or category.
    /// </summary>
    [HttpPost]
    [Route("{id}/assign")]
    public async Task<ActionResult<TransactionDto>> AssignTransaction(Guid id, [FromBody] AssignTransactionRequest request)
    {
        try
        {
            var result = await _assignmentService.AssignTransactionAsync(id, request);
            return Ok(result);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                title = "Conflict - Resource was modified.",
                status = 409,
                detail = "The transaction was modified by another request. Please reload and try again."
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                title = "Not Found",
                status = 404,
                detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning transaction {TransactionId}", id);
            return StatusCode(500, new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "Internal Server Error",
                status = 500
            });
        }
    }

    /// <summary>
    /// Splits a transaction across multiple budget lines or categories.
    /// </summary>
    [HttpPost]
    [Route("{id}/split")]
    public async Task<ActionResult<TransactionDto>> SplitTransaction(Guid id, [FromBody] SplitTransactionRequest request)
    {
        try
        {
            var result = await _assignmentService.SplitTransactionAsync(id, request);
            return Ok(result);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                title = "Conflict - Resource was modified.",
                status = 409,
                detail = "The transaction was modified by another request. Please reload and try again."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Validation error",
                status = 400,
                detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error splitting transaction {TransactionId}", id);
            return StatusCode(500, new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "Internal Server Error",
                status = 500
            });
        }
    }

    /// <summary>
    /// Bulk assigns multiple transactions to the same budget line or category.
    /// </summary>
    [HttpPost]
    [Route("bulk-assign")]
    public async Task<ActionResult<BulkAssignResultDto>> BulkAssignTransactions([FromBody] BulkAssignTransactionsRequest request)
    {
        try
        {
            var result = await _assignmentService.BulkAssignTransactionsAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk assigning transactions");
            return StatusCode(500, new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "Internal Server Error",
                status = 500
            });
        }
    }

    /// <summary>
    /// Undoes the last assignment action on a transaction.
    /// </summary>
    [HttpPost]
    [Route("undo")]
    public async Task<ActionResult<TransactionDto>> UndoAssignment([FromBody] UndoAssignmentRequest request)
    {
        try
        {
            var result = await _assignmentService.UndoAssignmentAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                title = "Not Found",
                status = 404,
                detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error undoing assignment");
            return StatusCode(500, new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "Internal Server Error",
                status = 500
            });
        }
    }

    /// <summary>
    /// Gets audit history for a transaction.
    /// </summary>
    [HttpGet]
    [Route("{id}/audit")]
    public async Task<ActionResult<List<TransactionAuditDto>>> GetAuditHistory(Guid id)
    {
        try
        {
            var audits = await _assignmentService.GetTransactionAuditHistoryAsync(id);
            return Ok(audits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit history for transaction {TransactionId}", id);
            return StatusCode(500, new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "Internal Server Error",
                status = 500
            });
        }
    }
}

/// <summary>
/// Request for previewing import file.
/// </summary>
public class PreviewImportRequest
{
    public string FileFormat { get; set; } = "csv";
    public string FileContent { get; set; } = string.Empty;
}
