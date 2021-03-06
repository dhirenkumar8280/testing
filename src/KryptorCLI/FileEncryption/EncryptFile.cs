using System;
using Sodium;
using System.IO;
using ChaCha20BLAKE2;
using System.Security.Cryptography;

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
    public static class EncryptFile
    {
        public static void Initialize(string inputFilePath, string outputFilePath, byte[] ephemeralPublicKey, byte[] salt, byte[] keyEncryptionKey)
        {
            byte[] dataEncryptionKey = Generate.DataEncryptionKey();
            try
            {
                using (var inputFile = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, Constants.FileStreamBufferSize, FileOptions.SequentialScan))
                using (var outputFile = new FileStream(outputFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, Constants.FileStreamBufferSize, FileOptions.SequentialScan))
                {
                    byte[] nonce = Generate.Nonce();
                    byte[] encryptedHeader = EncryptFileHeader(inputFilePath, ephemeralPublicKey, dataEncryptionKey, nonce, keyEncryptionKey);
                    FileHeaders.WriteHeaders(outputFile, ephemeralPublicKey, salt, nonce, encryptedHeader);
                    nonce = Utilities.Increment(nonce);
                    byte[] additionalData = ChunkHandling.GetPreviousTag(encryptedHeader);
                    Encrypt(inputFile, outputFile, nonce, dataEncryptionKey, additionalData);
                }
                Finalize(inputFilePath, outputFilePath);
            }
            catch (Exception ex) when (ExceptionFilters.Cryptography(ex))
            {
                FileHandling.DeleteFile(outputFilePath);
                CryptographicOperations.ZeroMemory(dataEncryptionKey);
                throw;
            }
        }

        private static byte[] EncryptFileHeader(string inputFilePath, byte[] ephemeralPublicKey, byte[] dataEncryptionKey, byte[] nonce, byte[] keyEncryptionKey)
        {
            long fileLength = FileHandling.GetFileLength(inputFilePath);
            byte[] lastChunkLength = BitConversion.GetBytes(Convert.ToInt32(fileLength % Constants.FileChunkSize));
            byte[] fileNameLength = FileHeaders.GetFileNameLength(inputFilePath);
            byte[] fileHeader = Arrays.Concat(lastChunkLength, fileNameLength, dataEncryptionKey);
            byte[] additionalData = HeaderEncryption.ComputeAdditionalData(fileLength, ephemeralPublicKey);
            return HeaderEncryption.Encrypt(fileHeader, nonce, keyEncryptionKey, additionalData);
        }

        private static void Encrypt(FileStream inputFile, FileStream outputFile, byte[] nonce, byte[] dataEncryptionKey, byte[] additionalData)
        {
            const int offset = 0;
            byte[] plaintextChunk = new byte[Constants.FileChunkSize];
            while (inputFile.Read(plaintextChunk, offset, plaintextChunk.Length) > 0)
            {
                byte[] ciphertextChunk = XChaCha20BLAKE2b.Encrypt(plaintextChunk, nonce, dataEncryptionKey, additionalData, TagLength.Medium);
                nonce = Utilities.Increment(nonce);
                additionalData = ChunkHandling.GetPreviousTag(ciphertextChunk);
                outputFile.Write(ciphertextChunk, offset, ciphertextChunk.Length);
            }
            CryptographicOperations.ZeroMemory(dataEncryptionKey);
        }

        private static void Finalize(string inputFilePath, string outputFilePath)
        {
            if (Globals.Overwrite)
            {
                FileHandling.OverwriteFile(inputFilePath, outputFilePath);
            }
            else if (Globals.ObfuscateFileNames)
            {
                RestoreFileName.RemoveAppendedFileName(inputFilePath);
            }
            FileHandling.SetFileAttributesReadOnly(outputFilePath);
        }
    }
}
