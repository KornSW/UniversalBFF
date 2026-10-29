using System;
using System.Security.Cryptography;
using System.Text;

namespace UniversalBFF.OobModules.UserManagement {

  /// <summary>
  /// Encrypts and decrypts the upstream token which is wrapped into a BFF-issued JWT ('wrp' claim).
  /// Uses AES-256-CBC with encrypt-then-MAC (HMAC-SHA256). Encryption and MAC keys are derived
  /// per token from a random salt ('slt' claim) and the internal BFF encryption secret.
  /// Only APIs available on .NET Framework 4.8 are used.
  /// </summary>
  internal static class WrappedTokenProtector {

    private const int _IvLength = 16;
    private const int _MacLength = 32;

    /// <summary>
    /// Creates a new random salt (URL-safe Base64) for one token.
    /// </summary>
    public static string CreateSalt() {
      return ToBase64Url(BffTokenSecrets.CreateRandomBytes(16));
    }

    /// <summary>
    /// Encrypts the given plain token and returns a URL-safe Base64 payload (IV | cipher | MAC).
    /// </summary>
    public static string Protect(string plainToken, string salt, byte[] secret) {
      byte[] encryptionKey = DeriveKey(secret, "enc|" + salt);
      byte[] macKey = DeriveKey(secret, "mac|" + salt);
      byte[] iv = BffTokenSecrets.CreateRandomBytes(_IvLength);
      byte[] cipher;

      using (Aes aes = Aes.Create()) {
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = encryptionKey;
        aes.IV = iv;
        using (ICryptoTransform encryptor = aes.CreateEncryptor()) {
          byte[] plainBytes = Encoding.UTF8.GetBytes(plainToken);
          cipher = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        }
      }

      byte[] ivAndCipher = new byte[iv.Length + cipher.Length];
      Buffer.BlockCopy(iv, 0, ivAndCipher, 0, iv.Length);
      Buffer.BlockCopy(cipher, 0, ivAndCipher, iv.Length, cipher.Length);

      byte[] mac;
      using (HMACSHA256 hmac = new HMACSHA256(macKey)) {
        mac = hmac.ComputeHash(ivAndCipher);
      }

      byte[] result = new byte[ivAndCipher.Length + mac.Length];
      Buffer.BlockCopy(ivAndCipher, 0, result, 0, ivAndCipher.Length);
      Buffer.BlockCopy(mac, 0, result, ivAndCipher.Length, mac.Length);

      return ToBase64Url(result);
    }

    /// <summary>
    /// Verifies the MAC and decrypts the payload. Fails closed (returns false) on any mismatch.
    /// </summary>
    public static bool TryUnprotect(string protectedToken, string salt, byte[] secret, out string plainToken) {
      plainToken = null;

      if (string.IsNullOrWhiteSpace(protectedToken) || string.IsNullOrWhiteSpace(salt)) {
        return false;
      }

      byte[] payload;
      try {
        payload = FromBase64Url(protectedToken);
      }
      catch (FormatException) {
        return false;
      }

      if (payload.Length < _IvLength + 16 + _MacLength) {
        return false;
      }

      int ivAndCipherLength = payload.Length - _MacLength;
      byte[] encryptionKey = DeriveKey(secret, "enc|" + salt);
      byte[] macKey = DeriveKey(secret, "mac|" + salt);

      byte[] expectedMac;
      using (HMACSHA256 hmac = new HMACSHA256(macKey)) {
        expectedMac = hmac.ComputeHash(payload, 0, ivAndCipherLength);
      }
      if (!FixedTimeEquals(expectedMac, 0, payload, ivAndCipherLength, _MacLength)) {
        return false;
      }

      byte[] iv = new byte[_IvLength];
      Buffer.BlockCopy(payload, 0, iv, 0, _IvLength);

      try {
        using (Aes aes = Aes.Create()) {
          aes.KeySize = 256;
          aes.Mode = CipherMode.CBC;
          aes.Padding = PaddingMode.PKCS7;
          aes.Key = encryptionKey;
          aes.IV = iv;
          using (ICryptoTransform decryptor = aes.CreateDecryptor()) {
            byte[] plainBytes = decryptor.TransformFinalBlock(payload, _IvLength, ivAndCipherLength - _IvLength);
            plainToken = Encoding.UTF8.GetString(plainBytes);
          }
        }
      }
      catch (CryptographicException) {
        plainToken = null;
        return false;
      }

      return true;
    }

    /// <summary>
    /// Derives a 256-bit key from the secret and a purpose-specific label.
    /// </summary>
    private static byte[] DeriveKey(byte[] secret, string label) {
      using (HMACSHA256 hmac = new HMACSHA256(secret)) {
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(label));
      }
    }

    /// <summary>
    /// Compares two byte ranges in constant time.
    /// </summary>
    private static bool FixedTimeEquals(byte[] left, int leftOffset, byte[] right, int rightOffset, int length) {
      int difference = 0;
      for (int i = 0; i < length; i++) {
        difference |= left[leftOffset + i] ^ right[rightOffset + i];
      }
      return (difference == 0);
    }

    private static string ToBase64Url(byte[] data) {
      return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] FromBase64Url(string data) {
      string base64 = data.Replace('-', '+').Replace('_', '/');
      int padding = base64.Length % 4;
      if (padding == 2) {
        base64 = base64 + "==";
      }
      else if (padding == 3) {
        base64 = base64 + "=";
      }
      return Convert.FromBase64String(base64);
    }

  }

}
