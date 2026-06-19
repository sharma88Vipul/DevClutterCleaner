namespace DevClutterCleaner.Domain;

public sealed record LicenseStatus(
    ProductEdition Edition,
    bool TelemetryEnabled,
    bool EnterprisePolicySyncEnabled,
    bool RoleSeparationEnabled)
{
    public static LicenseStatus Community { get; } = new(
        ProductEdition.Community,
        TelemetryEnabled: false,
        EnterprisePolicySyncEnabled: false,
        RoleSeparationEnabled: false);
}
