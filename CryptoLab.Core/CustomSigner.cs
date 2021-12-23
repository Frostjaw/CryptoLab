namespace CryptoLab.Core
{
    using CryptoLab.Core.Models;
    using System;
    using System.Security.Cryptography;

    public class CustomSigner : IDisposable
    {
        private readonly RSACryptoServiceProvider _rsaCryptoServiceProvider;

        public CustomSigner(string rsaParameters)
        {
            _rsaCryptoServiceProvider = new RSACryptoServiceProvider();
            _rsaCryptoServiceProvider.FromXmlString(rsaParameters);
        }

        public CustomSigner(RSAParameters rsaParameters)
        {
            _rsaCryptoServiceProvider = new RSACryptoServiceProvider();
            _rsaCryptoServiceProvider.ImportParameters(rsaParameters);
        }

        public static RSAParameters GenerateRsaParameters()
        {
            using var rsaCryptoServiceProvider = new RSACryptoServiceProvider();
            return rsaCryptoServiceProvider.ExportParameters(true);
        }

        public static string GenerateKeys()
        {
            using var rsaCryptoServiceProvider = new RSACryptoServiceProvider();
            return rsaCryptoServiceProvider.ToXmlString(true);
        }

        public byte[] CreateSignature(Transaction transaction)
        {
            var hash = Utils.ComputeSha256Hash(Utils.ObjectToByteArray(transaction));
            var encryptedHash = _rsaCryptoServiceProvider.SignHash(hash, CryptoConfig.MapNameToOID("SHA256"));

            return encryptedHash;
        }

        public void Dispose()
        {
            _rsaCryptoServiceProvider?.Dispose();
        }
    }
}

