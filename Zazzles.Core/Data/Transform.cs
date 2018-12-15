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
using System.Text;

namespace Zazzles.Data
{
    /// <summary>
    ///     Handle all encryption/decryption
    /// </summary>
    public static class Transform
    {
        /// <summary>
        ///     Base64 encode a string
        /// </summary>
        /// <param name="toEncode">The string that will be encoded</param>
        /// <returns>A base64 encoded string</returns>
        public static string EncodeBase64(string toEncode)
        {
            if (string.IsNullOrEmpty(toEncode))
                throw new ArgumentException("toEncode must be provided!", nameof(toEncode));

            var bytes = Encoding.ASCII.GetBytes(toEncode);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        ///     Decodes a base64 encoded string
        /// </summary>
        /// <param name="toDecode">A base64 encoded string</param>
        /// <returns>Returns the base64 decoded string</returns>
        public static string DecodeBase64(string toDecode)
        {
            if (string.IsNullOrEmpty(toDecode))
                throw new ArgumentException("toEncode must be provided!", nameof(toDecode));

            var bytes = Convert.FromBase64String(toDecode);
            return Encoding.ASCII.GetString(bytes);
        }

        /// <summary>
        ///     Converts a byte array to a hex string
        /// </summary>
        /// <param name="ba">The byte array to be converted</param>
        /// <returns>A hex string representation of the byte array</returns>
        public static string ByteArrayToHexString(byte[] ba)
        {
            if (ba == null)
                throw new ArgumentNullException(nameof(ba));

            var hex = new StringBuilder(ba.Length*2);
            foreach (var b in ba)
                hex.AppendFormat("{0:x2}", b);

            return hex.ToString();
        }

        /// <summary>
        ///     Converts a hex string to a byte array
        /// </summary>
        /// <param name="hex">The hex string to be converted</param>
        /// <returns>A byte array representation of the hex string</returns>
        public static byte[] HexStringToByteArray(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                throw new ArgumentException("A hex string must be provided!", nameof(hex));

            var numberChars = hex.Length;
            var bytes = new byte[numberChars/2];
            for (var i = 0; i < numberChars; i += 2)
                bytes[i/2] = Convert.ToByte(hex.Substring(i, 2), 16);

            return bytes;
        }
    }
}
