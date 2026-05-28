using System;
using System.IO;
using System.Text;
//using Newtonsoft.Json;
using System.Text.Json;
using AppContractsSCO.Configuration;
using AppContractsSCO.Services.Security;

namespace TemplateHost.Services.Security
{
    public class SecureSettingsLoader : ISecureSettingsLoader
    {
        private readonly ISecureConfig _secureConfig;

        public SecureSettingsLoader(ISecureConfig secureConfig)
        {
            _secureConfig = secureConfig;
        }

        public SecureSettings Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Missing encrypted config file: {path}");

            var json = File.ReadAllText(path, Encoding.UTF8);
            /*
            var settings = JsonConvert.DeserializeObject<SecureSettings>(json)
                           ?? throw new Exception("Failed to deserialize SecureSettings");
            */
            var settings = JsonSerializer.Deserialize<SecureSettings>(json)
                           ?? throw new Exception("Failed to deserialize SecureSettings");

            var key = _secureConfig.GetKey();

            if (key == null || key.Length == 0)
            {
                File.AppendAllText(
                    "/tmp/plumspaces_log.txt",
                    "KEY IS NULL OR EMPTY" + Environment.NewLine);
            }

            // -------------------------
            // SMTP
            // -------------------------
            settings.Smtp.Username = AesCrypto.Decrypt(settings.Smtp.Username, key);
            settings.Smtp.Password = AesCrypto.Decrypt(settings.Smtp.Password, key);

            // -------------------------
            // AdminAuth (Dictionary-based)
            // -------------------------
            if (settings.AdminAuth?.Areas != null)
            {
                foreach (var area in settings.AdminAuth.Areas)
                {
                    if (area.Value == null)
                        continue;

                    area.Value.Username =
                        AesCrypto.Decrypt(area.Value.Username, key);

                    area.Value.Password =
                        AesCrypto.Decrypt(area.Value.Password, key);
                }
            }

            // -------------------------
            // Connection Strings
            // -------------------------
            settings.ConnectionStrings.PlumspacesDb =
                AesCrypto.Decrypt(settings.ConnectionStrings.PlumspacesDb, key);

            if (settings.LocalConnectionStrings != null)
            {
                settings.LocalConnectionStrings.PlumspacesDb =
                    AesCrypto.Decrypt(settings.LocalConnectionStrings.PlumspacesDb, key);
            }

            return settings;
        }
    }
}