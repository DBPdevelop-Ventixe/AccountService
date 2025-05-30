using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;

namespace WebApi.Services;

public class CustomSecretManager : KeyVaultSecretManager
{
    private readonly string _prefix;

    public CustomSecretManager(string prefix)
    {
        _prefix = $"{prefix}-";
    }

    public override bool Load(SecretProperties secret)
    {
        // Check if the secret name starts with the specified prefix
        return secret.Name.StartsWith(_prefix);
    }

    public override string GetKey(KeyVaultSecret secret)
    {
        // Remove the prefix from the secret name
        var result = secret.Name[_prefix.Length..].Replace("--", ConfigurationPath.KeyDelimiter);
        return result;
    }
}
