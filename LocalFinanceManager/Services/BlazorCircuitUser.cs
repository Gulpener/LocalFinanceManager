namespace LocalFinanceManager.Services;

/// <summary>
/// Scoped service that holds the resolved user ID for the current Blazor Server circuit.
/// Populated once at circuit start from the authentication state, because
/// IHttpContextAccessor.HttpContext is null after the initial HTTP handshake in Blazor Server.
/// </summary>
public interface IBlazorCircuitUser
{
    Guid UserId { get; }
    string Email { get; }
    bool IsInitialized { get; }
    void Initialize(Guid userId, string email);
    void Reset();
}

public class BlazorCircuitUser : IBlazorCircuitUser
{
    public Guid UserId { get; private set; } = Guid.Empty;
    public string Email { get; private set; } = string.Empty;
    public bool IsInitialized { get; private set; }

    public void Initialize(Guid userId, string email)
    {
        UserId = userId;
        Email = email;
        IsInitialized = true;
    }

    public void Reset()
    {
        UserId = Guid.Empty;
        Email = string.Empty;
        IsInitialized = false;
    }
}
