using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Senstay.Dojo.Helpers
{
    /// <summary>
    /// basic Encrption/decryption functionaility
    /// code is copied from this link (error has been fixed):
    /// http://www.codeproject.com/Questions/304187/how-to-create-password-encrypt-decrypt-in-csharp
    /// generate encrypted secret samples:
    /// string secret = SecretGenerator.Encrypt(password, CryptoHelper.CryptoTypes.encTypeTripleDES);
    /// string secret = SecretGenerator.Encrypt(password, CryptoHelper.CryptoTypes.encTypeRijndael);
    /// string hash = SecretGenerator.Hash(password, Hashing.HashingTypes.MD5);
    /// string hash = SecretGenerator.Hash(password, Hashing.HashingTypes.SHA256);
    /// string hash = SecretGenerator.Hash(password, Hashing.HashingTypes.SHA512);
    /// </summary>
    public class CryptoHelper
    {
        #region enums, constants & fields
        //types of symmetric encyption
        public enum CryptoTypes
        {
            encTypeDES = 0,
            encTypeRC2,
            encTypeRijndael,
            encTypeTripleDES,
            base64
        }

        public enum HashingTypes
        {
            SHA, SHA256, SHA384, SHA512, MD5
        }

        private const string CRYPT_DEFAULT_KEY = "SenstayDojoTool";
        private const CryptoTypes CRYPT_DEFAULT_METHOD = CryptoTypes.encTypeRijndael;

        private byte[] mKey = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };
        private byte[] mIV = { 65, 110, 68, 26, 69, 178, 200, 219 };
        private byte[] SaltByteArray = { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 };
        private CryptoTypes mCryptoType = CRYPT_DEFAULT_METHOD;
        private string mSecretKey = CRYPT_DEFAULT_KEY;
        #endregion

        #region Constructors

        public CryptoHelper()
        {
            calculateNewKeyAndIV();
        }

        public CryptoHelper(CryptoTypes CryptoType)
        {
            this.CryptoType = CryptoType;
            calculateNewKeyAndIV();
        }
        #endregion

        #region Props

        /// <summary>
        ///     type of encryption / decryption used
        /// </summary>
        public CryptoTypes CryptoType
        {
            get
            {
                return mCryptoType;
            }
            set
            {
                if (mCryptoType != value)
                {
                    mCryptoType = value;
                    calculateNewKeyAndIV();
                }
            }
        }

        /// <summary>
        ///     Passsword Key Property.
        ///     The password key used when encrypting / decrypting
        /// </summary>
        public string SecretKey
        {
            get
            {
                return mSecretKey;
            }
            set
            {
                if (mSecretKey != value)
                {
                    mSecretKey = value;
                    calculateNewKeyAndIV();
                }
            }
        }
        #endregion

        #region Encryption

        /// <summary>
        ///     Encrypt a string using default key and crypto type
        /// </summary>
        /// <param name="inputText">text to encrypt</param>
        /// <returns>an encrypted string</returns>
        public string Encrypt(string inputText)
        {
            //declare a new encoder
            UTF8Encoding UTF8Encoder = new UTF8Encoding();
            //get byte representation of string
            byte[] inputBytes = UTF8Encoder.GetBytes(inputText);

            //convert back to a string
            return Convert.ToBase64String(EncryptDecrypt(inputBytes, true));
        }

        /// <summary>
        ///     Encrypt string with user defined key
        /// </summary>
        /// <param name="inputText">text to encrypt</param>
        /// <param name="secretKey">key to use when encrypting</param>
        /// <returns>an encrypted string</returns>
        public string Encrypt(string inputText, string secretKey)
        {
            this.SecretKey = secretKey;
            return this.Encrypt(inputText);
        }

        /// <summary>
        ///     Encrypt string using cryptoType with user defined key
        /// </summary>
        /// <param name="inputText">text to encrypt</param>
        /// <param name="secretKey">key to use when encrypting</param>
        /// <param name="cryptoType">type of encryption</param>
        /// <returns>an encrypted string</returns>
        public string Encrypt(string inputText, string secretKey, CryptoTypes cryptoType)
        {
            mCryptoType = cryptoType;
            return this.Encrypt(inputText, secretKey);
        }

        /// <summary>
        ///     Encrypt string using cryptoType
        /// </summary>
        /// <param name="inputText">text to encrypt</param>
        /// <param name="cryptoType">type of encryption</param>
        /// <returns>an encrypted string</returns>
        public string Encrypt(string inputText, CryptoTypes cryptoType)
        {
            this.CryptoType = cryptoType;
            return this.Encrypt(inputText);
        }

        #endregion

        #region Decryption
        /// <summary>
        ///     decrypts a string
        /// </summary>
        /// <param name="inputText">string to decrypt</param>
        /// <returns>a decrypted string</returns>
        public string Decrypt(string inputText)
        {
            //declare a new encoder
            UTF8Encoding UTF8Encoder = new UTF8Encoding();
            //get byte representation of string
            byte[] inputBytes = Convert.FromBase64String(inputText);

            //convert back to a string
            return UTF8Encoder.GetString(EncryptDecrypt(inputBytes, false));
        }

        /// <summary>
        ///     decrypts a string using a user defined password key
        /// </summary>
        /// <param name="inputText">string to decrypt</param>
        /// <param name="password">password to use when decrypting</param>
        /// <returns>a decrypted string</returns>
        public string Decrypt(string inputText, string secretkey)
        {
            this.SecretKey = secretkey;
            return Decrypt(inputText);
        }

        /// <summary>
        ///     decrypts a string usi decryption type with user defined key
        /// </summary>
        /// <param name="inputText">string to decrypt</param>
        /// <param name="secretKey">key key used to decrypt</param>
        /// <param name="cryptoType">type of decryption</param>
        /// <returns>a decrypted string</returns>
        public string Decrypt(string inputText, string secretKey, CryptoTypes cryptoType)
        {
            mCryptoType = cryptoType;
            return Decrypt(inputText, secretKey);
        }

        /// <summary>
        ///     decrypts a string using the decryption type
        /// </summary>
        /// <param name="inputText">string to decrypt</param>
        /// <param name="cryptoType">type of decryption</param>
        /// <returns>a decrypted string</returns>
        public string Decrypt(string inputText, CryptoTypes cryptoType)
        {
            this.CryptoType = cryptoType;
            return Decrypt(inputText);
        }
        #endregion

        #region Symmetric Engine

        /// <summary>
        ///     performs the actual encryption and decryption
        /// </summary>
        /// <param name="inputBytes">input byte array</param>
        /// <param name="Encrpyt">wheather or not to perform enc/dec</param>
        /// <returns>byte array output</returns>
        private byte[] EncryptDecrypt(byte[] inputBytes, bool Encrpyt)
        {
            //get the correct transform
            ICryptoTransform transform = getCryptoTransform(Encrpyt);

            //memory stream for output
            MemoryStream memStream = new MemoryStream();

            try
            {
                //setup the cryption - output written to memstream
                CryptoStream cryptStream = new CryptoStream(memStream, transform, CryptoStreamMode.Write);

                //write data to cryption engine
                cryptStream.Write(inputBytes, 0, inputBytes.Length);

                //we are finished
                cryptStream.FlushFinalBlock();

                //get result
                byte[] output = memStream.ToArray();

                //finished with engine, so close the stream
                cryptStream.Close();

                return output;
            }
            catch (Exception e)
            {
                //throw an error
                throw new Exception("Error in symmetric engine. Error : " + e.Message, e);
            }
        }

        /// <summary>
        ///     returns the symmetric engine and creates the encyptor/decryptor
        /// </summary>
        /// <param name="encrypt">whether to return a encrpytor or decryptor</param>
        /// <returns>ICryptoTransform</returns>
        private ICryptoTransform getCryptoTransform(bool encrypt)
        {
            SymmetricAlgorithm SA = selectAlgorithm();
            SA.Key = mKey;
            SA.IV = mIV;
            if (encrypt)
            {
                return SA.CreateEncryptor();
            }
            else
            {
                return SA.CreateDecryptor();
            }
        }

        /// <summary>
        ///     returns the specific symmetric algorithm acc. to the cryptotype
        /// </summary>
        /// <returns>SymmetricAlgorithm</returns>
        private SymmetricAlgorithm selectAlgorithm()
        {
            SymmetricAlgorithm SA;
            switch (mCryptoType)
            {
                case CryptoTypes.encTypeDES:
                    SA = DES.Create();
                    break;
                case CryptoTypes.encTypeRC2:
                    SA = RC2.Create();
                    break;
                case CryptoTypes.encTypeRijndael:
                    SA = Rijndael.Create();
                    break;
                case CryptoTypes.encTypeTripleDES:
                    SA = TripleDES.Create();
                    break;
                default:
                    SA = TripleDES.Create();
                    break;
            }
            return SA;
        }

        /// <summary>
        ///     calculates the key and IV acc. to the symmetric method from the password
        ///     key and IV size dependant on symmetric method
        /// </summary>
        private void calculateNewKeyAndIV()
        {
            //use salt so that key cannot be found with dictionary attack
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(mSecretKey, SaltByteArray);
            SymmetricAlgorithm algo = selectAlgorithm();
            mKey = pdb.GetBytes(algo.KeySize / 8);
            mIV = pdb.GetBytes(algo.BlockSize / 8);
        }

        #endregion

        #region Hash key generator

        #region static members
        public static string Hash(String inputText)
        {
            return ComputeHash(inputText, HashingTypes.MD5);
        }

        public static string Hash(String inputText, HashingTypes hashingType)
        {
            return ComputeHash(inputText, hashingType);
        }

        /// <summary>
        ///     returns true if the input text is equal to hashed text
        /// </summary>
        /// <param name="inputText">unhashed text to test</param>
        /// <param name="hashText">already hashed text</param>
        /// <returns>boolean true or false</returns>
        public static bool isHashEqual(string inputText, string hashText)
        {
            return (Hash(inputText) == hashText);
        }

        public static bool isHashEqual(string inputText, string hashText, HashingTypes hashingType)
        {
            return (Hash(inputText, hashingType) == hashText);
        }
        #endregion

        /// <summary>
        ///     computes the hash code and converts it to string
        /// </summary>
        /// <param name="inputText">input text to be hashed</param>
        /// <param name="hashingType">type of hashing to use</param>
        /// <returns>hashed string</returns>
        private static string ComputeHash(string inputText, HashingTypes hashingType)
        {
            HashAlgorithm HA = getHashAlgorithm(hashingType);

            //declare a new encoder
            UTF8Encoding UTF8Encoder = new UTF8Encoding();
            //get byte representation of input text
            byte[] inputBytes = UTF8Encoder.GetBytes(inputText);


            //hash the input byte array
            byte[] output = HA.ComputeHash(inputBytes);

            //convert output byte array to a string
            return Convert.ToBase64String(output);
        }

        /// <summary>
        ///     returns the specific hashing alorithm
        /// </summary>
        /// <param name="hashingType">type of hashing to use</param>
        /// <returns>HashAlgorithm</returns>
        private static HashAlgorithm getHashAlgorithm(HashingTypes hashingType)
        {
            switch (hashingType)
            {
                case HashingTypes.MD5:
                    return new MD5CryptoServiceProvider();
                case HashingTypes.SHA:
                    return new SHA1CryptoServiceProvider();
                case HashingTypes.SHA256:
                    return new SHA256Managed();
                case HashingTypes.SHA384:
                    return new SHA384Managed();
                case HashingTypes.SHA512:
                    return new SHA512Managed();
                default:
                    return new MD5CryptoServiceProvider();
            }
        }
        #endregion
    }

    public static class SecretGenerator
    {
        public static string Encrypt(string input, CryptoHelper.CryptoTypes encType = CryptoHelper.CryptoTypes.encTypeTripleDES)
        {
            CryptoHelper c = new CryptoHelper(encType);
            string s1 = c.Encrypt(input, encType);
            string s2 = c.Decrypt(s1, encType);
            if (s2 == input)
                return s1;
            else
                return null;
        }

        public static string Hash(string input, CryptoHelper.HashingTypes hashType = CryptoHelper.HashingTypes.MD5)
        {
            string s = CryptoHelper.Hash(input, hashType);
            if (CryptoHelper.isHashEqual(input, s, hashType))
                return s;
            else
                return null;
        }

        public static string Base64(string input)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(input);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }

    public static class SecretDecipher
    {
        public static string Decrypt(string secret, CryptoHelper.CryptoTypes encType = CryptoHelper.CryptoTypes.encTypeTripleDES)
        {
            if (encType == CryptoHelper.CryptoTypes.base64)
            {
                var base64EncodedBytes = System.Convert.FromBase64String(secret);
                return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            }
            else
            {
                CryptoHelper c = new CryptoHelper(encType);
                return c.Decrypt(secret, encType);
            }
        }
    }
}