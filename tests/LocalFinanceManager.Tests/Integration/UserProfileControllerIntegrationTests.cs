using FluentValidation;
using LocalFinanceManager.Configuration;
using LocalFinanceManager.Controllers;
using LocalFinanceManager.Data;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.DTOs.Validators;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using LocalFinanceManager.Tests.Fixtures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace LocalFinanceManager.Tests.Integration;

/// <summary>
/// Integration tests for UserProfileController covering profile CRUD and image validation.
/// Note: [Authorize] is not enforced when the controller is instantiated directly;
/// authorization is covered by E2E tests.
/// </summary>
[TestFixture]
public class UserProfileControllerIntegrationTests
{
    private static readonly Guid UserId = Guid.Parse("aaaabbbb-0000-0000-0000-000000000001");

    private SqliteConnection _connection = null!;
    private AppDbContext _context = null!;
    private UserPreferencesService _prefsService = null!;
    private Mock<ISupabaseStorageService> _storageMock = null!;
    private UserProfileController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _context = CreateContext();
        _context.Database.EnsureCreated();

        // Seed the owning user so FK constraints are satisfied
        _context.Users.Add(new User
        {
            Id = UserId,
            SupabaseUserId = UserId.ToString(),
            Email = "test@profile.local",
            DisplayName = "Test User"
        });
        _context.SaveChanges();

        var factory = new ConnectionDbContextFactory(_connection);
        _prefsService = new UserPreferencesService(factory, _context, new Mock<ILogger<UserPreferencesService>>().Object);

        _storageMock = new Mock<ISupabaseStorageService>();
        _storageMock.Setup(s => s.GetPublicUrl(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns((string bucket, string path) => $"https://example.supabase.co/storage/v1/object/public/{bucket}/{path}");

        var supabaseOptions = Options.Create(new SupabaseOptions
        {
            Url = "https://example.supabase.co",
            AnonKey = "test-anon-key",
            StorageBucket = "profile-pictures"
        });

        IValidator<UpdateProfileRequest> validator = new UpdateProfileRequestValidator();
        var logger = new Mock<ILogger<UserProfileController>>().Object;

        _controller = new UserProfileController(
            _prefsService,
            _storageMock.Object,
            new TestUserContext(UserId),
            validator,
            supabaseOptions,
            logger);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new AppDbContext(options);
    }

    // ── GET /api/profile ─────────────────────────────────────────────────────

    [Test]
    public async Task GetProfile_NoPrefs_ReturnsOkWithEmptyProfile()
    {
        var result = await _controller.GetProfile(CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        var profile = (UserProfileResponse)ok.Value!;
        Assert.That(profile.FirstName, Is.Null);
        Assert.That(profile.LastName, Is.Null);
        Assert.That(profile.ProfileImageUrl, Is.Null);
    }

    [Test]
    public async Task GetProfile_WithNameAndImage_ReturnsProfileWithUrl()
    {
        _context.UserPreferences.Add(new UserPreferences
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            FirstName = "Alice",
            LastName = "Smith",
            ProfileImagePath = $"{UserId}/avatar.jpg"
        });
        await _context.SaveChangesAsync();

        var result = await _controller.GetProfile(CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        var profile = (UserProfileResponse)ok.Value!;
        Assert.That(profile.FirstName, Is.EqualTo("Alice"));
        Assert.That(profile.LastName, Is.EqualTo("Smith"));
        Assert.That(profile.ProfileImageUrl, Does.Contain("profile-pictures"));
    }

    // ── PUT /api/profile ──────────────────────────────────────────────────────

    [Test]
    public async Task UpdateProfile_ValidRequest_SavesAndReturnsOk()
    {
        var request = new UpdateProfileRequest { FirstName = "Bob", LastName = "Jones" };

        var result = await _controller.UpdateProfile(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());

        var prefs = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == UserId && !p.IsArchived);
        Assert.That(prefs, Is.Not.Null);
        Assert.That(prefs!.FirstName, Is.EqualTo("Bob"));
        Assert.That(prefs.LastName, Is.EqualTo("Jones"));
    }

    [Test]
    public async Task UpdateProfile_FirstNameTooLong_ReturnsBadRequest()
    {
        var request = new UpdateProfileRequest { FirstName = new string('A', 101) };

        var result = await _controller.UpdateProfile(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objResult = (ObjectResult)result;
        Assert.That(objResult.StatusCode, Is.EqualTo(400));
    }

    // ── POST /api/profile/picture – magic byte validation ────────────────────

    [Test]
    public async Task UploadProfilePicture_InvalidMagicBytes_ReturnsBadRequest()
    {
        // Create a fake file with invalid bytes (not a valid image)
        var invalidBytes = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 };
        var file = CreateFormFile(invalidBytes, "fake.jpg", "image/jpeg");

        _controller.ControllerContext.HttpContext.Request.Headers.Authorization = "Bearer test-jwt-token";

        var result = await _controller.UploadProfilePicture(file, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UploadProfilePicture_ValidJpegHeader_CallsStorage()
    {
        // JPEG magic bytes: FF D8 + some valid content
        var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01 };
        var file = CreateFormFile(jpegBytes, "photo.jpg", "image/jpeg");

        _controller.ControllerContext.HttpContext.Request.Headers.Authorization = "Bearer test-jwt-token";

        _storageMock.Setup(s => s.UploadAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync($"{UserId}/avatar.jpg");

        var result = await _controller.UploadProfilePicture(file, CancellationToken.None);

        _storageMock.Verify(s => s.UploadAsync(
            "profile-pictures", It.IsAny<string>(), It.IsAny<Stream>(),
            "image/jpeg", "test-jwt-token", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UploadProfilePicture_ValidPngHeader_Accepted()
    {
        // PNG magic bytes: 89 50 4E 47 0D 0A 1A 0A
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };
        var file = CreateFormFile(pngBytes, "photo.png", "image/png");

        _controller.ControllerContext.HttpContext.Request.Headers.Authorization = "Bearer test-jwt-token";

        _storageMock.Setup(s => s.UploadAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync($"{UserId}/avatar.png");

        var result = await _controller.UploadProfilePicture(file, CancellationToken.None);

        // Should not be a bad request
        Assert.That(result.Result, Is.Not.InstanceOf<BadRequestObjectResult>());
    }

    // ── DELETE /api/profile/picture ──────────────────────────────────────────

    [Test]
    public async Task DeleteProfilePicture_NoImageSet_Returns404()
    {
        _controller.ControllerContext.HttpContext.Request.Headers.Authorization = "Bearer test-jwt-token";

        var result = await _controller.DeleteProfilePicture(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task DeleteProfilePicture_ImageExists_DeletesAndReturns204()
    {
        _context.UserPreferences.Add(new UserPreferences
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            ProfileImagePath = $"{UserId}/avatar.jpg"
        });
        await _context.SaveChangesAsync();

        _controller.ControllerContext.HttpContext.Request.Headers.Authorization = "Bearer test-jwt-token";

        _storageMock.Setup(s => s.DeleteAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.DeleteProfilePicture(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());

        var prefs = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == UserId && !p.IsArchived);
        Assert.That(prefs!.ProfileImagePath, Is.Null);
    }

    // ── POST /api/profile/picture – Authorization header validation ──────────

    [Test]
    public async Task UploadProfilePicture_MissingAuthorizationHeader_Returns401()
    {
        var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01 };
        var file = CreateFormFile(jpegBytes, "photo.jpg", "image/jpeg");

        // No Authorization header set
        var result = await _controller.UploadProfilePicture(file, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task UploadProfilePicture_InvalidAuthorizationHeader_Returns401()
    {
        var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01 };
        var file = CreateFormFile(jpegBytes, "photo.jpg", "image/jpeg");

        // Malformed — not "Bearer <token>"
        _controller.ControllerContext.HttpContext.Request.Headers.Authorization = "Basic dXNlcjpwYXNz";

        var result = await _controller.UploadProfilePicture(file, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task DeleteProfilePicture_MissingAuthorizationHeader_Returns401()
    {
        _context.UserPreferences.Add(new UserPreferences
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            ProfileImagePath = $"{UserId}/avatar.jpg"
        });
        await _context.SaveChangesAsync();

        // No Authorization header set
        var result = await _controller.DeleteProfilePicture(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task DeleteProfilePicture_InvalidAuthorizationHeader_Returns401()
    {
        _context.UserPreferences.Add(new UserPreferences
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            ProfileImagePath = $"{UserId}/avatar.jpg"
        });
        await _context.SaveChangesAsync();

        // Malformed — not "Bearer <token>"
        _controller.ControllerContext.HttpContext.Request.Headers.Authorization = "Basic dXNlcjpwYXNz";

        var result = await _controller.DeleteProfilePicture(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static IFormFile CreateFormFile(byte[] content, string fileName, string contentType)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class ConnectionDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly SqliteConnection _connection;

        public ConnectionDbContextFactory(SqliteConnection connection) => _connection = connection;

        public AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;
            return new AppDbContext(options);
        }

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken ct = default)
            => Task.FromResult(CreateDbContext());
    }
}
