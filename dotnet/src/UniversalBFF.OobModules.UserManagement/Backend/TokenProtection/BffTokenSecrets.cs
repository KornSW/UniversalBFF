using Logging.SmartStandards;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace UniversalBFF.OobModules.UserManagement {

  /// <summary>
  /// Provides the two internal BFF secrets: one for signing BFF-issued JWTs and a separate one
  /// for encrypting the wrapped upstream token ('wrp').
  /// Resolution order: environment variables, then a persisted secrets file, which is generated
  /// automatically on first use (out-of-the-box usability for single-node setups).
  /// Multi-node deployments must provide the same values via environment variables on every node.
  /// </summary>
  internal static class BffTokenSecrets {

    public const string SigningSecretEnvVar = "UBFF_JWT_SIGNING_SECRET";
    public const string EncryptionSecretEnvVar = "UBFF_WRP_ENCRYPTION_SECRET";
    public const string SecretsFileEnvVar = "UBFF_TOKEN_SECRETS_FILE";

    private const string _SigningKeyName = "jwtSigningSecret";
    private const string _EncryptionKeyName = "wrpEncryptionSecret";

    private static readonly object _SyncRoot = new object();
    private static byte[] _SigningSecret = null;
    private static byte[] _EncryptionSecret = null;

    /// <summary>
    /// The HMAC key used to sign and verify BFF-issued JWTs.
    /// </summary>
    public static byte[] SigningSecret {
      get {
        EnsureLoaded();
        return _SigningSecret;
      }
    }

    /// <summary>
    /// The secret from which per-token keys for the 'wrp' encryption are derived.
    /// </summary>
    public static byte[] EncryptionSecret {
      get {
        EnsureLoaded();
        return _EncryptionSecret;
      }
    }

    /// <summary>
    /// Loads both secrets exactly once (thread-safe).
    /// </summary>
    private static void EnsureLoaded() {
      if (_SigningSecret != null && _EncryptionSecret != null) {
        return;
      }
      lock (_SyncRoot) {
        if (_SigningSecret != null && _EncryptionSecret != null) {
          return;
        }

        string signingFromEnv = Environment.GetEnvironmentVariable(SigningSecretEnvVar);
        string encryptionFromEnv = Environment.GetEnvironmentVariable(EncryptionSecretEnvVar);

        Dictionary<string, string> persisted = null;
        if (string.IsNullOrWhiteSpace(signingFromEnv) || string.IsNullOrWhiteSpace(encryptionFromEnv)) {
          persisted = LoadOrCreateSecretsFile();
        }

        if (!string.IsNullOrWhiteSpace(signingFromEnv)) {
          _SigningSecret = Convert.FromBase64String(signingFromEnv);
        }
        else {
          _SigningSecret = Convert.FromBase64String(persisted[_SigningKeyName]);
        }

        if (!string.IsNullOrWhiteSpace(encryptionFromEnv)) {
          _EncryptionSecret = Convert.FromBase64String(encryptionFromEnv);
        }
        else {
          _EncryptionSecret = Convert.FromBase64String(persisted[_EncryptionKeyName]);
        }
      }
    }

    /// <summary>
    /// Reads the secrets file or creates it with fresh random secrets if it does not exist.
    /// </summary>
    private static Dictionary<string, string> LoadOrCreateSecretsFile() {
      string fileName = GetSecretsFileName();

      if (File.Exists(fileName)) {
        Dictionary<string, string> existing = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(fileName));
        if (existing != null && existing.ContainsKey(_SigningKeyName) && existing.ContainsKey(_EncryptionKeyName)) {
          return existing;
        }
      }

      Dictionary<string, string> created = new Dictionary<string, string>();
      created[_SigningKeyName] = Convert.ToBase64String(CreateRandomBytes(32));
      created[_EncryptionKeyName] = Convert.ToBase64String(CreateRandomBytes(32));

      string directory = Path.GetDirectoryName(fileName);
      if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory)) {
        Directory.CreateDirectory(directory);
      }
      File.WriteAllText(fileName, JsonConvert.SerializeObject(created, Formatting.Indented));

      SecLogger.LogWarning(2085310457913180161L, 77310, "New BFF token secrets have been generated at '" + fileName + "'. All previously issued BFF tokens are invalid now.");
      return created;
    }

    /// <summary>
    /// Determines the location of the persisted secrets file.
    /// </summary>
    private static string GetSecretsFileName() {
      string configured = Environment.GetEnvironmentVariable(SecretsFileEnvVar);
      if (!string.IsNullOrWhiteSpace(configured)) {
        return configured;
      }
      string baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
      if (string.IsNullOrWhiteSpace(baseDirectory)) {
        baseDirectory = AppContext.BaseDirectory;
      }
      return Path.Combine(baseDirectory, "UniversalBFF", "token-secrets.json");
    }

    /// <summary>
    /// Creates cryptographically strong random bytes.
    /// </summary>
    internal static byte[] CreateRandomBytes(int length) {
      byte[] buffer = new byte[length];
      using (RandomNumberGenerator rng = RandomNumberGenerator.Create()) {
        rng.GetBytes(buffer);
      }
      return buffer;
    }

  }

}
