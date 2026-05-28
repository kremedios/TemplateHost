using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using AppContractsSCO.Services.Security;

namespace TemplateHost.Services.Security
{


public class SecureConfig : ISecureConfig
{

    private readonly byte[] _key;

    public SecureConfig()
    {
        _key = LoadKey(); // load once
    }


    /**
    Use this method with the actual file containing the AES key.  This method works with both 
    Windows and Linux deployment.

    Trying to put the AES key into the environment value in Windows brings up all sorts of 
    very obsure problems with insertion of invisble characters into the string representing
    the key even without changing its length.  Visible inspection shows no discrepancy.  Only
    doing a SHA256 hash shows the inconsistency of the key put into the Windows environment.
    This probably has something to do with the editor in the copy and paste operations--my
    speculation.

    So using actual key file deployment is the most robust cross-platform approach.
    */
    private byte[] LoadKey()
    {
        //This is where the key is stored.
        string keyPath = Path.Combine(
            AppContext.BaseDirectory,
            "secure",
            //"plumspaces_aes.key");
             "chummy.key");

        if (!File.Exists(keyPath))
            throw new Exception($"AES key file not found: {keyPath}");

        var base64Key = File.ReadAllText(keyPath).Trim();

        if (string.IsNullOrWhiteSpace(base64Key))
            throw new Exception("AES key file is empty.");

        base64Key = new string(base64Key.Where(c =>
            char.IsLetterOrDigit(c) || c == '+' || c == '/' || c == '='
        ).ToArray());

        return Convert.FromBase64String(base64Key);
    }

    public byte[] GetKey() => _key;

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
            return cipherText;

        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;

        var iv = new byte[16];
        Array.Copy(fullCipher, 0, iv, 0, iv.Length);
        aes.IV = iv;

        var cipher = new byte[fullCipher.Length - iv.Length];
        Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(cipher);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }
}




}
