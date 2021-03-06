﻿using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Xyfy.Helper
{
    /// <summary>
    /// 
    /// </summary>
    public partial class RSAHelper
    {
        private readonly RSA _privateKeyRsaProvider;
        private readonly RSA _publicKeyRsaProvider;
        private readonly Encoding _encoding;
#if NET45
        private readonly HashAlgorithm _sh;
#endif
#if  NETSTANDARD2_0
        private readonly HashAlgorithmName _hashAlgorithmName;
#endif

        /// <summary>
        /// 实例化RSAHelper
        /// </summary>
        /// <param name="rsaType">加密算法类型 RSA SHA1;RSA2 SHA256 密钥长度至少为2048</param>
        /// <param name="encoding">编码类型</param>
        /// <param name="privateKey">私钥</param>
        /// <param name="publicKey">公钥</param>
        public RSAHelper(RSAType rsaType, Encoding encoding, string privateKey, string publicKey = null)
        {
            _encoding = encoding;
            if (!string.IsNullOrEmpty(privateKey))
            {
                _privateKeyRsaProvider = DecodePemPrivateKey(privateKey);
            }

            if (!string.IsNullOrEmpty(publicKey))
            {
                _publicKeyRsaProvider = DecodePemPublicKey(publicKey);
            }
#if NET45
            if (rsaType == RSAType.RSA)
            {
                _sh = new SHA1CryptoServiceProvider();
            }
            else
            {
                _sh = new SHA256CryptoServiceProvider();
            }
#endif

#if  NETSTANDARD2_0
            _hashAlgorithmName = rsaType == RSAType.RSA ? HashAlgorithmName.SHA1 : HashAlgorithmName.SHA256;
#endif
        }
        private static readonly object LockObj = new object();

        /// <summary>
        /// 签名
        /// </summary>
        /// <param name="content">待签名字符串</param>
        /// <returns>签名后字符串</returns>
        public string Sign(string content)
        {
            byte[] data = _encoding.GetBytes(content);
            byte[] signData;
            lock (LockObj)
            {
#if NETSTANDARD2_0
                signData = _privateKeyRsaProvider.SignData(data, _hashAlgorithmName, RSASignaturePadding.Pkcs1);
#endif
#if NET45
                signData = (_privateKeyRsaProvider as RSACryptoServiceProvider)?.SignData(data, _sh);
#endif
            }
            return Convert.ToBase64String(signData);
        }

        /// <summary>
        /// 验签
        /// </summary>
        /// <param name="content">待验签字符串</param>
        /// <param name="signedString">签名</param>
        /// <returns>true(通过)，false(不通过)</returns>
        public bool Verify(string content, string signedString)
        {
            bool result = false;
            byte[] dataBytes = _encoding.GetBytes(content);
            byte[] signBytes = Convert.FromBase64String(signedString);
            lock (LockObj)
            {
#if NETSTANDARD2_0
                result = _publicKeyRsaProvider.VerifyData(dataBytes, signBytes, _hashAlgorithmName,
                    RSASignaturePadding.Pkcs1);
#endif
#if NET45
            result = ((RSACryptoServiceProvider) _publicKeyRsaProvider).VerifyData(dataBytes, _sh, signBytes);
#endif
            }

            return result;
        }

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="resData">需要加密的字符串</param>
        /// <returns>明文</returns>
        public string Encrypt(string resData)
        {
            byte[] dataToEncrypt = _encoding.GetBytes(resData);
            string result = Encrypt(dataToEncrypt);
            return result;
        }


        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="resData">加密字符串</param>
        /// <returns>明文</returns>
        public string Decrypt(string resData)
        {
            byte[] dataToDecrypt = Convert.FromBase64String(resData);
            string result = "";
            for (int j = 0; j < dataToDecrypt.Length / 128; j++)
            {
                byte[] buf = new byte[128];
                for (int i = 0; i < 128; i++)
                {

                    buf[i] = dataToDecrypt[i + 128 * j];
                }
                result += Decrypt(buf);
            }
            return result;
        }


        #region 导入密钥算法

        private int GetIntegerSize(BinaryReader binr)
        {
            byte bt = 0;
            int count = 0;
            bt = binr.ReadByte();
            if (bt != 0x02)
                return 0;
            bt = binr.ReadByte();

            if (bt == 0x81)
                count = binr.ReadByte();
            else
            if (bt == 0x82)
            {
                var highbyte = binr.ReadByte();
                var lowbyte = binr.ReadByte();
                byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };
                count = BitConverter.ToInt32(modint, 0);
            }
            else
            {
                count = bt;
            }

            while (binr.ReadByte() == 0x00)
            {
                count -= 1;
            }
            binr.BaseStream.Seek(-1, SeekOrigin.Current);
            return count;
        }

        private bool CompareBytearrays(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            int i = 0;
            foreach (byte c in a)
            {
                if (c != b[i])
                    return false;
                i++;
            }
            return true;
        }

        #endregion


        #region 内部方法

        private string Encrypt(byte[] data)
        {
            byte[] result = _publicKeyRsaProvider.Encrypt(data, false);
            return Convert.ToBase64String(result);
        }

        private string Decrypt(byte[] data)
        {
            string result = "";
            byte[] source = new RSACryptoServiceProvider().Decrypt(data, false);
            char[] asciiChars = new char[_encoding.GetCharCount(source, 0, source.Length)];
            _encoding.GetChars(source, 0, source.Length, asciiChars, 0);
            result = new string(asciiChars);
            //result = ASCIIEncoding.ASCII.GetString(source);
            return result;
        }


        private RSA DecodePemPublicKey(String pemstr)
        {
            var pkcs8Publickkey = Convert.FromBase64String(pemstr);
            {
                RSA rsa = DecodeRsaPublicKey(pkcs8Publickkey);
                return rsa;
            }
        }

        private RSA GetRSA()
        {
#if  NETSTANDARD2_0
            var rsa = RSA.Create();
#endif
#if NET45
            var rsa = new RSACryptoServiceProvider();
#endif
            return rsa;
        }

        private RSA DecodePemPrivateKey(String privateKey)
        {
            var privateKeyBits = Convert.FromBase64String(privateKey);
            var rsa = GetRSA();

            var rsaParameters = new RSAParameters();

            using (BinaryReader binr = new BinaryReader(new MemoryStream(privateKeyBits)))
            {
                byte bt = 0;
                ushort twobytes = 0;
                twobytes = binr.ReadUInt16();
                if (twobytes == 0x8130)
                    binr.ReadByte();
                else if (twobytes == 0x8230)
                    binr.ReadInt16();
                else
                    throw new Exception("Unexpected value read binr.ReadUInt16()");

                twobytes = binr.ReadUInt16();
                if (twobytes != 0x0102)
                    throw new Exception("Unexpected version");

                bt = binr.ReadByte();
                if (bt != 0x00)
                    throw new Exception("Unexpected value read binr.ReadByte()");

                rsaParameters.Modulus = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.Exponent = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.D = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.P = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.Q = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.DP = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.DQ = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.InverseQ = binr.ReadBytes(GetIntegerSize(binr));
            }

            rsa.ImportParameters(rsaParameters);
            return rsa;
        }


        private RSA DecodeRsaPublicKey(byte[] publickey)
        {
            // encoded OID sequence for  PKCS #1 rsaEncryption szOID_RSA_RSA = "1.2.840.113549.1.1.1"
            byte[] SeqOID = { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 };
            byte[] seq = new byte[15];
            // ---------  Set up stream to read the asn.1 encoded SubjectPublicKeyInfo blob  ------
            MemoryStream mem = new MemoryStream(publickey);
            BinaryReader binr = new BinaryReader(mem);    //wrap Memory Stream with BinaryReader for easy reading
            byte bt = 0;
            ushort twobytes = 0;

            try
            {

                twobytes = binr.ReadUInt16();
                if (twobytes == 0x8130) //data read as little endian order (actual data order for Sequence is 30 81)
                    binr.ReadByte();    //advance 1 byte
                else if (twobytes == 0x8230)
                    binr.ReadInt16();   //advance 2 bytes
                else
                    return null;

                seq = binr.ReadBytes(15);       //read the Sequence OID
                if (!CompareBytearrays(seq, SeqOID))    //make sure Sequence for OID is correct
                    return null;

                twobytes = binr.ReadUInt16();
                if (twobytes == 0x8103) //data read as little endian order (actual data order for Bit String is 03 81)
                    binr.ReadByte();    //advance 1 byte
                else if (twobytes == 0x8203)
                    binr.ReadInt16();   //advance 2 bytes
                else
                    return null;

                bt = binr.ReadByte();
                if (bt != 0x00)     //expect null byte next
                    return null;

                twobytes = binr.ReadUInt16();
                if (twobytes == 0x8130) //data read as little endian order (actual data order for Sequence is 30 81)
                    binr.ReadByte();    //advance 1 byte
                else if (twobytes == 0x8230)
                    binr.ReadInt16();   //advance 2 bytes
                else
                    return null;

                twobytes = binr.ReadUInt16();
                byte lowbyte = 0x00;
                byte highbyte = 0x00;

                if (twobytes == 0x8102) //data read as little endian order (actual data order for Integer is 02 81)
                    lowbyte = binr.ReadByte();  // read next bytes which is bytes in modulus
                else if (twobytes == 0x8202)
                {
                    highbyte = binr.ReadByte(); //advance 2 bytes
                    lowbyte = binr.ReadByte();
                }
                else
                    return null;
                byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };   //reverse byte order since asn.1 key uses big endian order
                int modsize = BitConverter.ToInt32(modint, 0);

                byte firstbyte = binr.ReadByte();
                binr.BaseStream.Seek(-1, SeekOrigin.Current);

                if (firstbyte == 0x00)
                {   //if first byte (highest order) of modulus is zero, don't include it
                    binr.ReadByte();    //skip this null byte
                    modsize -= 1;   //reduce modulus buffer size by 1
                }

                byte[] modulus = binr.ReadBytes(modsize);   //read the modulus bytes

                if (binr.ReadByte() != 0x02)            //expect an Integer for the exponent data
                    return null;
                int expbytes = (int)binr.ReadByte();        // should only need one byte for actual exponent data (for all useful values)
                byte[] exponent = binr.ReadBytes(expbytes);

                // ------- create RSACryptoServiceProvider instance and initialize with public key -----
                var rsa = GetRSA();
                RSAParameters RSAKeyInfo = new RSAParameters();
                RSAKeyInfo.Modulus = modulus;
                RSAKeyInfo.Exponent = exponent;
                rsa.ImportParameters(RSAKeyInfo);
                return rsa;
            }
            catch (Exception)
            {
                return null;
            }

            finally { binr.Close(); }

        }
        #endregion

        #region 解析.net 生成的Pem
        private static RSAParameters ConvertFromPublicKey(string pemFileConent)
        {

            byte[] keyData = Convert.FromBase64String(pemFileConent);
            if (keyData.Length < 162)
            {
                throw new ArgumentException("pem file content is incorrect.");
            }
            byte[] pemModulus = new byte[128];
            byte[] pemPublicExponent = new byte[3];
            Array.Copy(keyData, 29, pemModulus, 0, 128);
            Array.Copy(keyData, 159, pemPublicExponent, 0, 3);
            RSAParameters para = new RSAParameters();
            para.Modulus = pemModulus;
            para.Exponent = pemPublicExponent;
            return para;
        }

        private static RSAParameters ConvertFromPrivateKey(string pemFileConent)
        {
            byte[] keyData = Convert.FromBase64String(pemFileConent);
            if (keyData.Length < 609)
            {
                throw new ArgumentException("pem file content is incorrect.");
            }

            int index = 11;
            byte[] pemModulus = new byte[128];
            Array.Copy(keyData, index, pemModulus, 0, 128);

            index += 128;
            index += 2;//141
            byte[] pemPublicExponent = new byte[3];
            Array.Copy(keyData, index, pemPublicExponent, 0, 3);

            index += 3;
            index += 4;//148
            byte[] pemPrivateExponent = new byte[128];
            Array.Copy(keyData, index, pemPrivateExponent, 0, 128);

            index += 128;
            index += ((int)keyData[index + 1] == 64 ? 2 : 3);//279
            byte[] pemPrime1 = new byte[64];
            Array.Copy(keyData, index, pemPrime1, 0, 64);

            index += 64;
            index += ((int)keyData[index + 1] == 64 ? 2 : 3);//346
            byte[] pemPrime2 = new byte[64];
            Array.Copy(keyData, index, pemPrime2, 0, 64);

            index += 64;
            index += ((int)keyData[index + 1] == 64 ? 2 : 3);//412/413
            byte[] pemExponent1 = new byte[64];
            Array.Copy(keyData, index, pemExponent1, 0, 64);

            index += 64;
            index += ((int)keyData[index + 1] == 64 ? 2 : 3);//479/480
            byte[] pemExponent2 = new byte[64];
            Array.Copy(keyData, index, pemExponent2, 0, 64);

            index += 64;
            index += ((int)keyData[index + 1] == 64 ? 2 : 3);//545/546
            byte[] pemCoefficient = new byte[64];
            Array.Copy(keyData, index, pemCoefficient, 0, 64);

            RSAParameters para = new RSAParameters();
            para.Modulus = pemModulus;
            para.Exponent = pemPublicExponent;
            para.D = pemPrivateExponent;
            para.P = pemPrime1;
            para.Q = pemPrime2;
            para.DP = pemExponent1;
            para.DQ = pemExponent2;
            para.InverseQ = pemCoefficient;
            return para;
        }
        #endregion

    }

    static class Rsa
    {
        internal static byte[] Decrypt(this RSA rsa, byte[] rgb, bool fOAEP)
        {
#if  NETSTANDARD2_0
            return rsa.Decrypt(rgb, RSAEncryptionPadding.Pkcs1);
#endif
#if NET45
            if (rsa is RSACryptoServiceProvider t)
            {
                return t.Decrypt(rgb, fOAEP);
            }
            return new Byte[0];
#endif
        }


        internal static byte[] Encrypt(this RSA rsa, byte[] rgb, bool fOAEP)
        {
#if  NETSTANDARD2_0
            return rsa.Encrypt(rgb, RSAEncryptionPadding.Pkcs1);
#endif
#if NET45
            if (rsa is RSACryptoServiceProvider t)
            {
                return t.Encrypt(rgb, fOAEP);
            }
            return new Byte[0];
#endif
        }
    }
}