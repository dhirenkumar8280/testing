using System;
using System.Security.Cryptography;
using ChaCha20BLAKE2;

/*
    Kryptor: A simple, modern, and secure encryption tool.
    Copyright(C) 2020-2021 Samuel Lucas

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program. If not, see https://www.gnu.org/licenses/.
*/

namespace KryptorCLI
{
    public static class PrivateKey
    {
        public static byte[] Encrypt(byte[] passwordBytes, byte[] keyAlgorithm, byte[] privateKey)
        {
            byte[] salt = Generate.Salt();
            byte[] key = Argon2.DeriveKey(passwordBytes, salt);
            CryptographicOperations.ZeroMemory(passwordBytes);
            byte[] nonce = Generate.Nonce();
            byte[] additionalData = Arrays.Concat(keyAlgorithm, Constants.PrivateKeyVersion);
            byte[] encryptedPrivateKey = XChaCha20BLAKE2b.Encrypt(privateKey, nonce, key, additionalData, TagLength.Medium);
            CryptographicOperations.ZeroMemory(privateKey);
            CryptographicOperations.ZeroMemory(key);
            return Arrays.Concat(additionalData, salt, nonce, encryptedPrivateKey);
        }

        public static byte[] Decrypt(byte[] privateKey)
        {
            try
            {
                char[] password = PasswordPrompt.EnterYourPassword();
                byte[] passwordBytes = Password.Hash(password);
                return Decrypt(passwordBytes, privateKey);
            }
            catch (CryptographicException)
            {
                DisplayMessage.Error("Incorrect password or the private key has been tampered with.");
                return null;
            }
        }

        private static byte[] Decrypt(byte[] passwordBytes, byte[] privateKey)
        {
            byte[] keyAlgorithm = GetKeyAlgorithm(privateKey);
            byte[] keyVersion = GetKeyVersion(privateKey);
            byte[] salt = GetSalt(privateKey);
            byte[] nonce = GetNonce(privateKey);
            byte[] encryptedPrivateKey = GetEncryptedPrivateKey(privateKey);
            byte[] additionalData = Arrays.Concat(keyAlgorithm, keyVersion);
            byte[] key = Argon2.DeriveKey(passwordBytes, salt);
            CryptographicOperations.ZeroMemory(passwordBytes);
            byte[] decryptedPrivateKey = XChaCha20BLAKE2b.Decrypt(encryptedPrivateKey, nonce, key, additionalData, TagLength.Medium);
            CryptographicOperations.ZeroMemory(key);
            return decryptedPrivateKey;
        }

        private static byte[] GetKeyAlgorithm(byte[] privateKey)
        {
            byte[] keyAlgorithm = new byte[Constants.Curve25519KeyHeader.Length];
            Array.Copy(privateKey, keyAlgorithm, keyAlgorithm.Length);
            return keyAlgorithm;
        }

        private static byte[] GetKeyVersion(byte[] privateKey)
        {
            byte[] keyVersion = new byte[Constants.PrivateKeyVersion.Length];
            Array.Copy(privateKey, Constants.Curve25519KeyHeader.Length, keyVersion, destinationIndex: 0, keyVersion.Length);
            return keyVersion;
        }

        private static byte[] GetSalt(byte[] privateKey)
        {
            byte[] salt = new byte[Constants.SaltLength];
            int sourceIndex = Constants.Curve25519KeyHeader.Length + Constants.PrivateKeyVersion.Length;
            Array.Copy(privateKey, sourceIndex, salt, destinationIndex: 0, salt.Length);
            return salt;
        }

        private static byte[] GetNonce(byte[] privateKey)
        {
            byte[] nonce = new byte[Constants.XChaChaNonceLength];
            int sourceIndex = Constants.Curve25519KeyHeader.Length + Constants.PrivateKeyVersion.Length + Constants.SaltLength;
            Array.Copy(privateKey, sourceIndex, nonce, destinationIndex: 0, nonce.Length);
            return nonce;
        }

        private static byte[] GetEncryptedPrivateKey(byte[] privateKey)
        {
            int sourceIndex = Constants.Curve25519KeyHeader.Length + Constants.PrivateKeyVersion.Length + Constants.SaltLength + Constants.XChaChaNonceLength;
            byte[] encryptedPrivateKey = new byte[privateKey.Length - sourceIndex];
            Array.Copy(privateKey, sourceIndex, encryptedPrivateKey, destinationIndex: 0, encryptedPrivateKey.Length);
            return encryptedPrivateKey;
        }
    }
}
