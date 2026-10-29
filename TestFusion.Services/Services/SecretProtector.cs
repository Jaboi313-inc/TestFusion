using Microsoft.AspNetCore.DataProtection;
using TestFusion.Core.Interfaces;

namespace TestFusion.Services.Services;

public class SecretProtector : ISecretProtector
{
    private readonly IDataProtector _protector;

    public SecretProtector(
        IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("TestFusion.CarbonXCredentials.v1");
    }

    public string Protect(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return _protector.Protect(value);
    }

    public string Unprotect(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return _protector.Unprotect(value);
    }
}