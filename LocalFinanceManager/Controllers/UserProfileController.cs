using FluentValidation;
using LocalFinanceManager.Configuration;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LocalFinanceManager.Controllers;

/// <summary>
/// API controller for user profile management: name, profile picture.
/// </summary>
[Authorize]
[ApiController]
[Route("api/profile")]
public class UserProfileController : ControllerBase
{
    private readonly IUserPreferencesService _prefsService;
    private readonly ISupabaseStorageService _storageService;
    private readonly IUserContext _userContext;
    private readonly IValidator<UpdateProfileRequest> _updateValidator;
    private readonly SupabaseOptions _supabaseOptions;
    private readonly ILogger<UserProfileController> _logger;

    public UserProfileController(
        IUserPreferencesService prefsService,
        ISupabaseStorageService storageService,
        IUserContext userContext,
        IValidator<UpdateProfileRequest> updateValidator,
        IOptions<SupabaseOptions> supabaseOptions,
        ILogger<UserProfileController> logger)
    {
        _prefsService = prefsService;
        _storageService = storageService;
        _userContext = userContext;
        _updateValidator = updateValidator;
        _supabaseOptions = supabaseOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Returns the current user's profile including their public profile picture URL.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> GetProfile(CancellationToken ct)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        try
        {
            var profile = await _prefsService.GetProfileAsync(userId);
            var imagePath = await _prefsService.GetProfileImagePathAsync(userId);

            if (imagePath is not null)
                profile.ProfileImageUrl = _storageService.GetPublicUrl(_supabaseOptions.StorageBucket, imagePath);

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving profile for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails { Status = 500, Title = "Unexpected error" });
        }
    }

    /// <summary>
    /// Updates the current user's first name and last name.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        try
        {
            await _prefsService.UpdateProfileAsync(userId, request.FirstName, request.LastName);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails { Status = 500, Title = "Unexpected error" });
        }
    }

    /// <summary>
    /// Uploads a profile picture. Accepts JPEG, PNG, and WebP up to 2 MB.
    /// </summary>
    [HttpPost("picture")]
    [RequestSizeLimit(2_097_152)]
    [RequestFormLimits(MultipartBodyLengthLimit = 2_097_152)]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> UploadProfilePicture(
        [FromForm] IFormFile file,
        CancellationToken ct)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        if (file is null || file.Length == 0)
            return BadRequest(new ProblemDetails { Status = 400, Title = "No file provided" });

        // Magic byte validation
        var (isValid, contentType, extension) = await ValidateImageMagicBytesAsync(file);
        if (!isValid)
            return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid image format. Only JPEG, PNG, and WebP are allowed." });

        // Get JWT from Authorization header
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (authHeader is null || !authHeader.StartsWith("Bearer "))
            return Unauthorized();
        var jwt = authHeader["Bearer ".Length..];

        try
        {
            // Delete old picture if one exists
            var oldPath = await _prefsService.GetProfileImagePathAsync(userId);
            if (oldPath is not null)
            {
                try
                {
                    await _storageService.DeleteAsync(_supabaseOptions.StorageBucket, oldPath, jwt, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old profile picture {Path} for user {UserId}", oldPath, userId);
                }
            }

            // Upload new picture
            var newPath = $"{userId}/avatar{extension}";
            await using var stream = file.OpenReadStream();
            await _storageService.UploadAsync(_supabaseOptions.StorageBucket, newPath, stream, contentType, jwt, ct);

            await _prefsService.UpdateProfileImagePathAsync(userId, newPath);

            var imageUrl = _storageService.GetPublicUrl(_supabaseOptions.StorageBucket, newPath);
            return Ok(new UserProfileResponse { ProfileImageUrl = imageUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile picture for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails { Status = 500, Title = "Unexpected error during upload" });
        }
    }

    /// <summary>
    /// Removes the current user's profile picture.
    /// </summary>
    [HttpDelete("picture")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProfilePicture(CancellationToken ct)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var imagePath = await _prefsService.GetProfileImagePathAsync(userId);
        if (imagePath is null)
            return NotFound(new ProblemDetails { Status = 404, Title = "No profile picture set" });

        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (authHeader is null || !authHeader.StartsWith("Bearer "))
            return Unauthorized();
        var jwt = authHeader["Bearer ".Length..];

        try
        {
            await _storageService.DeleteAsync(_supabaseOptions.StorageBucket, imagePath, jwt, ct);
            await _prefsService.UpdateProfileImagePathAsync(userId, null);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile picture for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails { Status = 500, Title = "Unexpected error during deletion" });
        }
    }

    private static async Task<(bool isValid, string contentType, string extension)> ValidateImageMagicBytesAsync(IFormFile file)
    {
        var buffer = new byte[12];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));

        if (bytesRead < 4)
            return (false, string.Empty, string.Empty);

        // JPEG: FF D8
        if (buffer[0] == 0xFF && buffer[1] == 0xD8)
            return (true, "image/jpeg", ".jpg");

        // PNG: 89 50 4E 47
        if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
            return (true, "image/png", ".png");

        // WebP: 52 49 46 46 ... 57 45 42 50 at bytes 8-11
        if (bytesRead >= 12 &&
            buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46 &&
            buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50)
            return (true, "image/webp", ".webp");

        return (false, string.Empty, string.Empty);
    }
}
