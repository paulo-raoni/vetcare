namespace VetCare.Api.Authorization;

internal static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";

    public const string VetOrAdmin = "VetOrAdmin";

    public const string AnyStaff = "AnyStaff";
}
