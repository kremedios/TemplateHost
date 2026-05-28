using System.Security.Cryptography;
using System.Text;

namespace TemplateHost.Services.Security;

public static class AesCrypto
{
    /**
    • Accepts Base64 encrypted value
    • Extracts IV
    • Uses AES key
    • Returns plaintext string

    Same format as your encryptor.
    */

public static string Decrypt(string cipherText, byte[] key)
{
    try
    {
        //File.AppendAllText(@"C:\Sites\Plumspaces_Log.txt",
        //    "-AesCrypto.cs: --- Decrypt Start ----\n");

        var fullCipher = Convert.FromBase64String(cipherText);

        var hash = SHA256.HashData(fullCipher);

        if (fullCipher.Length < 17)
            throw new Exception("Cipher too short to contain IV + data");

        var iv = new byte[16];
        var cipher = new byte[fullCipher.Length - 16];

        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        //File.AppendAllText(@"C:\Sites\Plumspaces_Log.txt",
        //    $"AesCrypto.cs: IV SHA256: {Convert.ToBase64String(SHA256.HashData(iv))}\n");

        //File.AppendAllText(@"C:\Sites\Plumspaces_Log.txt",
        //    $"AesCrypto.cs: Cipher SHA256: {Convert.ToBase64String(SHA256.HashData(cipher))}\n");

        //File.AppendAllText(@"C:\Sites\Plumspaces_Log.txt",
        //    $"AesCrypto.cs: Key SHA256: {Convert.ToBase64String(SHA256.HashData(key))}\n");


        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();

        byte[] decryptedBytes =
            decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

        var result = Encoding.UTF8.GetString(decryptedBytes);

        //File.AppendAllText(@"C:\Sites\Plumspaces_Log.txt",
        //    "---- Decrypt Success ----\n");

        return result;
    }
    catch (Exception ex)
    {
        File.AppendAllText(@"C:\Sites\Plumspaces_Log.txt",
            $"*** ERROR: {ex}\n");

        throw;
    }
}





}
