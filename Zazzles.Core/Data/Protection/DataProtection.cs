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
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace Zazzles.Data.Protection
{
    public class DataProtection
    {
        private readonly ILogger _logger;
        private const string SECRETS_DIR = "securedata";

        public DataProtection(ILogger logger)
        {
            _logger = logger;
        }
        /// <summary>
        ///     Securly protect bytes by using local credentials
        /// </summary>
        /// <param name="data">The bytes to protect</param>
        /// <param name="userScope">Encrypt the data as the current user (false means use the local machine)</param>
        /// <returns></returns>
    //    public static byte[] Protect(byte[] data, bool alterPermissions)
    //    {
    //        if(data == null)
    //            throw new ArgumentNullException(nameof(data));
    //        return ProtectedData.Protect(data, null,
     //           userScope ? DataProtectionScope.CurrentUser : DataProtectionScope.LocalMachine);
     //   }

        /// <summary>
        ///     Unprotect bytes by using local credentials
        /// </summary>
        /// <param name="data">The bytes to unprotect</param>
        /// <param name="userScope">Decrypt the data as the current user (false means use the local machine)</param>
        /// <returns></returns>
     //   public static byte[] UnProtect(byte[] data, bool userScope)
     //   {
     //       if (data == null)
     //           throw new ArgumentNullException(nameof(data));
     //
     //       return ProtectedData.Unprotect(data, null,
     //           userScope ? DataProtectionScope.CurrentUser : DataProtectionScope.LocalMachine);
     //   }
    }
}
