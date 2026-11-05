using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KitTecnicoPro.Models;
using KitTecnicoPro.Serialization;

namespace KitTecnicoPro.Services
{
    /// <summary>Serviço de cofre de credenciais com criptografia autenticada AES-256-CBC + HMAC-SHA256 e DPAPI.</summary>
    public static class VaultService
    {
        public const string DecryptionErrorMarker = "⚠ Erro ao descriptografar";

        private static readonly string VaultFile =
            Path.Combine(AppPaths.DataDir, "vault.enc");

        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("KitTecnicoPro_Vault_v2_Protection");
        private static readonly byte[] PortableSalt = Encoding.UTF8.GetBytes("KitTecnicoPro_Portable_Salt_v2");

        public static bool IsDecryptionError(string? val) => val == DecryptionErrorMarker;

        private static (byte[] encKey, byte[] macKey) GetPortableKeys()
        {
            byte[] derived = Rfc2898DeriveBytes.Pbkdf2(
                "KitTecnicoPro_PortableVault_v2_KeyMaterial",
                PortableSalt,
                100_000,
                HashAlgorithmName.SHA256,
                64);
            return (derived[..32], derived[32..64]);
        }

        private static byte[] GetLegacyKey()
        {
            string seed = GetMachineGuid() + "KitTecnicoPro_v1";
            return SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        }

        private static string GetMachineGuid()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine
                    .OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
                return key?.GetValue("MachineGuid")?.ToString() ?? Environment.MachineName;
            }
            catch { return Environment.MachineName; }
        }

        public static string Encrypt(string plain)
        {
            if (string.IsNullOrEmpty(plain) || IsDecryptionError(plain)) return "";
            try
            {
                var (encKey, macKey) = GetPortableKeys();
                byte[] iv = RandomNumberGenerator.GetBytes(16);
                using var aes = Aes.Create();
                aes.Key = encKey;
                aes.IV  = iv;
                byte[] cipherBytes = aes.EncryptCbc(Encoding.UTF8.GetBytes(plain), iv);

                byte[] payload = new byte[16 + cipherBytes.Length];
                iv.CopyTo(payload, 0);
                cipherBytes.CopyTo(payload, 16);

                byte[] tag = HMACSHA256.HashData(macKey, payload);
                byte[] finalBytes = new byte[payload.Length + tag.Length];
                payload.CopyTo(finalBytes, 0);
                tag.CopyTo(finalBytes, payload.Length);

                return "AES2:" + Convert.ToBase64String(finalBytes);
            }
            catch
            {
                byte[] data = Encoding.UTF8.GetBytes(plain);
                byte[] encrypted = ProtectedData.Protect(data, Entropy, DataProtectionScope.CurrentUser);
                return "DPAPI:" + Convert.ToBase64String(encrypted);
            }
        }

        public static string Decrypt(string cipher)
        {
            if (string.IsNullOrEmpty(cipher)) return "";
            try
            {
                if (cipher.StartsWith("AES2:"))
                {
                    byte[] raw = Convert.FromBase64String(cipher[5..]);
                    if (raw.Length < 48) return DecryptionErrorMarker;

                    var (encKey, macKey) = GetPortableKeys();
                    byte[] payload = raw[..^32];
                    byte[] tag     = raw[^32..];
                    byte[] expected = HMACSHA256.HashData(macKey, payload);
                    if (!CryptographicOperations.FixedTimeEquals(tag, expected))
                        return DecryptionErrorMarker;

                    byte[] iv  = payload[..16];
                    byte[] enc = payload[16..];
                    using var aes = Aes.Create();
                    aes.Key = encKey;
                    aes.IV  = iv;
                    return Encoding.UTF8.GetString(aes.DecryptCbc(enc, iv));
                }

                if (cipher.StartsWith("DPAPI:"))
                {
                    byte[] data = Convert.FromBase64String(cipher[6..]);
                    byte[] decrypted = ProtectedData.Unprotect(data, Entropy, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(decrypted);
                }

                byte[] legacyData = Convert.FromBase64String(cipher);
                byte[] ivLegacy = legacyData[..16];
                byte[] encLegacy = legacyData[16..];
                using var aesLegacy = Aes.Create();
                aesLegacy.Key = GetLegacyKey();
                aesLegacy.IV  = ivLegacy;
                return Encoding.UTF8.GetString(aesLegacy.DecryptCbc(encLegacy, ivLegacy));
            }
            catch
            {
                return DecryptionErrorMarker;
            }
        }

        public static List<CredentialEntry> Load()
        {
            if (!File.Exists(VaultFile)) return new();
            try
            {
                string raw  = File.ReadAllText(VaultFile, Encoding.UTF8);
                string json = Decrypt(raw);
                if (IsDecryptionError(json)) return new();
                return JsonSerializer.Deserialize(json, KitJsonCompactContext.Utf8.ListCredentialEntry) ?? new();
            }
            catch { return new(); }
        }

        public static void Save(List<CredentialEntry> entries)
        {
            Directory.CreateDirectory(AppPaths.DataDir);
            string json = JsonSerializer.Serialize(entries, KitJsonCompactContext.Utf8.ListCredentialEntry);
            string tmp  = $"{VaultFile}.{Guid.NewGuid():N}.tmp";
            try
            {
                File.WriteAllText(tmp, Encrypt(json), Encoding.UTF8);
                File.Move(tmp, VaultFile, overwrite: true);
            }
            finally
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            }
        }
    }
}
