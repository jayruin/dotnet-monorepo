namespace ksse.Users;

internal sealed class ChangePasswordRequest
{
    public required string CurrentPassword { get; init; }
    public required string NewPassword { get; init; }
    public bool ApplyClientHash { get; init; }
}
