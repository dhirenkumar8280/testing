﻿using System;
using System.Security.Cryptography;
using Sodium;

/*
    Kryptor: Free and open source file encryption software.
    Copyright(C) 2020 Samuel Lucas

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

namespace KryptorGUI
{
    public static class MemoryEncryption
    {
        // Memory encryption for Mono
        private static readonly KeyPair _keyPair = PublicKeyBox.GenerateKeyPair();

        public static void EncryptByteArray(ref byte[] byteArray)
        {
            try
            {
                if (Globals.MemoryEncryption == true && byteArray != null)
                {
                    if (Constants.RunningOnMono == false)
                    {
                        // Windows
                        ProtectedMemory.Protect(byteArray, MemoryProtectionScope.SameProcess);
                    }
                    else if (Constants.RunningOnMono == true)
                    {
                        // Linux & macOS
                        byteArray = SealedPublicKeyBox.Create(byteArray, _keyPair.PublicKey);
                    }
                }
            }
            catch (Exception ex) when (ExceptionFilters.MemoryEncryptionExceptions(ex))
            {
                Globals.MemoryEncryption = false;
                Settings.SaveSettings();
                Logging.LogException(ex.ToString(), Logging.Severity.Bug);
                DisplayMessage.ErrorMessageBox(ex.GetType().Name, "Memory encryption has been disabled due to an exception. This is a bug - please report it.");
            }
        }

        public static void DecryptByteArray(ref byte[] byteArray)
        {
            try
            {
                if (Globals.MemoryEncryption == true && byteArray != null)
                {
                    if (Constants.RunningOnMono == false)
                    {
                        // Windows
                        ProtectedMemory.Unprotect(byteArray, MemoryProtectionScope.SameProcess);
                    }
                    else if (Constants.RunningOnMono == true)
                    {
                        // Linux & macOS
                        byteArray = SealedPublicKeyBox.Open(byteArray, _keyPair);
                    }
                }
            }
            catch (Exception ex) when (ExceptionFilters.MemoryEncryptionExceptions(ex))
            {
                Logging.LogException(ex.ToString(), Logging.Severity.Bug);
                DisplayMessage.ErrorMessageBox(ex.GetType().Name, "Memory decryption failed. This is a bug - please report it.");
            }
        }
    }
}