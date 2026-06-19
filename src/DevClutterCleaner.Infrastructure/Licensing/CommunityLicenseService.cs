using DevClutterCleaner.Application.Abstractions;
using DevClutterCleaner.Domain;

namespace DevClutterCleaner.Infrastructure.Licensing;

public sealed class CommunityLicenseService : ILicenseService
{
    public LicenseStatus GetCurrentStatus() => LicenseStatus.Community;
}
