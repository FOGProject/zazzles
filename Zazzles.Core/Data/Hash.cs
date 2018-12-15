/*
    Copyright(c) 2014-2018 FOG Project

    The MIT License

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files(the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions :
    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
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