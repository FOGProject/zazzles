/*
 * Zazzles : A cross platform service framework
 * Copyright (C) 2014-2018 FOG Project
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


namespace Zazzles.Data
{
    /// <summary>
    ///     Handle all encryption/decryption
    /// </summary>
    public static class Hash
    {
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

            alg.ComputeHash(data);
            return BitConverter.ToString(alg.Hash).Replace("-", "");
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

            const int bufferSize = 1200000;

            using (var stream = new BufferedStream(File.OpenRead(filePath), bufferSize))
            {
                var checksum = alg.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", "");
            }
        }
    }
}