using System.Diagnostics.CodeAnalysis;

namespace Launcher.Models;

public sealed class LoginResponse
{
    public bool Success { get; set; }

    [MemberNotNullWhen(true, nameof(Success))]
    public string? SessionId { get; set; }

    public string? LaunchArguments { get; set; }
}