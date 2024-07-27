using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Identity;

namespace Reserve.API.Services;

public class EncryptionService
{
    private readonly KeyClient _keyClient;

    public EncryptionService(KeyClient keyClient)
    {
        _keyClient = keyClient;
    }

    public async Task<byte[]> EncryptAsync(string keyName, byte[] data)
    {
        KeyVaultKey key = await _keyClient.GetKeyAsync(keyName);
        var cryptoClient = new CryptographyClient(key.Id, new DefaultAzureCredential());
        EncryptResult result = await cryptoClient.EncryptAsync(EncryptionAlgorithm.RsaOaep, data);
        return result.Ciphertext;
    }

    public async Task<byte[]> DecryptAsync(string keyName, byte[] encryptedData)
    {
        KeyVaultKey key = await _keyClient.GetKeyAsync(keyName);
        var cryptoClient = new CryptographyClient(key.Id, new DefaultAzureCredential());
        DecryptResult result = await cryptoClient.DecryptAsync(EncryptionAlgorithm.RsaOaep, encryptedData);
        return result.Plaintext;
    }
}