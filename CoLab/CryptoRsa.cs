using System;
using System.IO;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace CoLab
{
    internal static class CryptoRsa
    {
        internal static Tuple<string, string> GenerateKeyPair()
        {
            var r = new RsaKeyPairGenerator();
            r.Init(new KeyGenerationParameters(new SecureRandom(), 2048));
            var kp = r.GenerateKeyPair();

            TextWriter textWriter = new StringWriter();
            var pemWriter = new PemWriter(textWriter);
            pemWriter.WriteObject(kp.Public);
            pemWriter.Writer.Flush();
            var publicKey = textWriter.ToString();

            textWriter = new StringWriter();
            pemWriter = new PemWriter(textWriter);
            pemWriter.WriteObject(kp.Private);
            pemWriter.Writer.Flush();
            var privateKey = textWriter.ToString();
            return new Tuple<string, string>(publicKey, privateKey);
        }

        internal static string Decrypt(string input, string privKey)
        {
            var data = Encoding.UTF8.GetBytes(input);
            var decryptEngine = new Pkcs1Encoding(new RsaEngine());
            using (var txtreader = new StringReader(privKey))
            {
                var keyPair = (AsymmetricCipherKeyPair)new PemReader(txtreader).ReadObject();
                decryptEngine.Init(false, keyPair.Private);
            }
            var decrypted = decryptEngine.ProcessBlock(data, 0, data.Length);
            return Encoding.UTF8.GetString(decrypted);
        }

        internal static byte[] Encrypt(byte[] data, string pubKey, int offset)
        {
            var encryptEngine = new Pkcs1Encoding(new RsaEngine());
            using (var txtreader = new StringReader(pubKey))
            {
                var keyParameter = (AsymmetricKeyParameter)new PemReader(txtreader).ReadObject();
                encryptEngine.Init(true, keyParameter);
            }
            var encrypted = encryptEngine.ProcessBlock(data, offset, data.Length);
            return encrypted;
        }
    }
}