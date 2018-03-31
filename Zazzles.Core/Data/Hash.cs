/*
 * Zazzles : A cross platform service framework
 * Copyright (C) 2014-2016 FOG Project
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 3
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.IO;
using System.Security.Cryptography;

// ReSharper disable InconsistentNaming

namespace Zazzles.Data
{
    /// <summary>
    ///     Handle all encryption/decryption
    /// </summary>
    public static class Hash
    {
        private const string LogName = "Data::Hash";

        /// <summary>
        ///     Creates an md5 hash of bytes
        /// </summary>
        /// <param name="data">The bytes to hash</param>
        /// <returns></returns>
        public static string MD5(byte[] data)
        {
            using (var alg = System.Security.Cryptography.MD5.Create())
                return HashBytes(alg, data);
        }

        /// <summary>
        ///     Creates an md5 hash of a file
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <returns></returns>
        public static string MD5(string filePath)
        {
            using (var alg = System.Security.Cryptography.MD5.Create())
                return HashFile(alg, filePath);
        }

        /// <summary>
        ///     Creates a sha1 hash of bytes
        /// </summary>
        /// <param name="data">The bytes to hash</param>
        /// <returns></returns>
        public static string SHA1(byte[] data)
        {
            using (var alg = System.Security.Cryptography.SHA1.Create())
                return HashBytes(alg, data);
        }

        /// <summary>
        ///     Creates a sha1 hash of a file
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <returns></returns>
        public static string SHA1(string filePath)
        {
            using (var alg = System.Security.Cryptography.SHA1.Create())
                return HashFile(alg, filePath);
        }

        /// <summary>
        ///     Creates a sha256 hash of bytes
        /// </summary>
        /// <param name="data">The bytes to hash</param>
        /// <returns></returns>
        public static string SHA256(byte[] data)
        {
            using (var alg = System.Security.Cryptography.SHA256.Create())
                return HashBytes(alg, data);
        }

        /// <summary>
        ///     Creates a sha256 hash of a file
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <returns></returns>
        public static string SHA256(string filePath)
        {
            using (var alg = System.Security.Cryptography.SHA256.Create())
                return HashFile(alg, filePath);
        }

        /// <summary>
        ///     Creates a sha384 hash of bytes
        /// </summary>
        /// <param name="data">The bytes to hash</param>
        /// <returns></returns>
        public static string SHA384(byte[] data)
        {
            using (var alg = System.Security.Cryptography.SHA384.Create())
                return HashBytes(alg, data);
        }

        /// <summary>
        ///     Creates a sha384 hash of a file
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <returns></returns>
        public static string SHA384(string filePath)
        {
            using (var alg = System.Security.Cryptography.SHA384.Create())
                return HashFile(alg, filePath);
        }

        /// <summary>
        ///     Creates a sha512 hash of bytes
        /// </summary>
        /// <param name="data">The bytes to hash</param>
        /// <returns></returns>
        public static string SHA512(byte[] data)
        {
            using (var alg = System.Security.Cryptography.SHA512.Create())
                return HashBytes(alg, data);
        }

        /// <summary>
        ///     Creates a sha512 hash of a file
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <returns></returns>
        public static string SHA512(string filePath)
        {
            using (var alg = System.Security.Cryptography.SHA512.Create())
                return HashFile(alg, filePath);
        }

        /// <summary>
        /// Hash a set of bytes with a given algorithm, digested to hex form
        /// </summary>
        /// <param name="alg">The hash to use</param>
        /// <param name="data">The bytes to hash</param>
        /// <returns>A hex encoded hash</returns>
        private static string HashBytes(HashAlgorithm alg, byte[] data)
        {
            if (alg == null)
                throw new ArgumentNullException(nameof(alg));
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            try
            {
                alg.ComputeHash(data);
                return BitConverter.ToString(alg.Hash).Replace("-", "");
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Unable to hash bytes");
                Log.Error(LogName, ex);
            }

            return null;
        }

        /// <summary>
        /// Hash a file with a given algorithm, digested to hex form
        /// </summary>
        /// <param name="alg">The hash to use</param>
        /// <param name="filePath">The file to hash</param>
        /// <returns>A hex encoded hash</returns>
        private static string HashFile(HashAlgorithm alg, string filePath)
        {
            if (alg == null)
                throw new ArgumentNullException(nameof(alg));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path required!", nameof(filePath));

            try
            {
                using (var stream = new BufferedStream(File.OpenRead(filePath), 1200000))
                {
                    var checksum = alg.ComputeHash(stream);
                    return BitConverter.ToString(checksum).Replace("-", "");
                }
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Unable to hash file: " + filePath);
                Log.Error(LogName, ex);
            }

            return null;
        }
    }
}