using DevClutterCleaner.Domain;

namespace DevClutterCleaner.Application.Abstractions;

public interface ILicenseService
{
    LicenseStatus GetCurrentStatus();
}
