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

/*
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using FOG.Handlers.Data;
using Zazzles.Data;
using System.Timers;
using RSA = Zazzles.Data.RSA;
using Timer = System.Timers.Timer;

// ReSharper disable InconsistentNaming

namespace Zazzles.Middleware
{
    public static class Authentication
    {
        private const string LogName = "Middleware::Authentication";
        private static byte[] Passkey;
        private static AutoResetEvent CanAuth;
        private static Timer EventTimer;

        static Authentication()
        {
            CanAuth = new AutoResetEvent(true);
            EventTimer = new Timer();

            EventTimer.Elapsed += onTimerEnd;
            EventTimer.AutoReset = false;
            EventTimer.Interval = 2*60*1000;
        }

        private static void onTimerEnd(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            CanAuth.Set();
        }

        /// <summary>
        ///     Generate a random AES pass key and securely send it to the server
        /// </summary>
        /// <returns>True if successfully authenticated</returns>
        public static bool HandShake()
        {
            try
            {
                Log.Entry(LogName, "Waiting for authentication timeout to pass");
                CanAuth.WaitOne();
                EventTimer.Start();

                // Obtain a public key from the server
                var keyPath = Path.Combine(Settings.Location, "tmp", "public.cer");
                Communication.DownloadFile("/management/other/ssl/srvpublic.crt", keyPath);
                Log.Debug(LogName, "KeyPath = " + keyPath);
                var certificate = new X509Certificate2(keyPath);

                // Ensure the public key came from the pinned server
                if (!Data.RSA.IsFromCA(Data.RSA.ServerCertificate(), certificate))
                    throw new Exception("Certificate is not from FOG CA");
                Log.Entry(LogName, "Cert OK");

                // Generate a random AES key
                var aes = new AesCryptoServiceProvider();
                aes.GenerateKey();
                Passkey = aes.Key;

                // Get the security token from the last handshake
                var tokenPath = Path.Combine(Settings.Location, "token.dat");

                try
                {
                    if (!File.Exists(tokenPath) && File.Exists("token.dat"))
                    {
                        File.Copy("token.dat", tokenPath);
                    }
                }
                catch (Exception)
                {
                }

                var token = GetSecurityToken(tokenPath);
                // Encrypt the security token and AES key using the public key
                var enKey = Transform.ByteArrayToHexString(RSA.Encrypt(certificate, Passkey));
                var enToken = Transform.ByteArrayToHexString(RSA.Encrypt(certificate, token));
                // Send the encrypted data to the server and get the response
                var response = Communication.Post("/management/index.php?sub=requestClientInfo&authorize&newService",
                    $"sym_key={enKey}&token={enToken}&mac={Configuration.MACAddresses()}");

                // If the server accepted the token and AES key, save the new token
                if (!response.Error && response.Encrypted)
                {
                    Log.Entry(LogName, "Authenticated");
                    SetSecurityToken(tokenPath, Transform.HexStringToByteArray(response.GetField("token")));
                    return true;
                }

                // If the server does not recognize the host, register it
                if (response.ReturnCode.Equals("ih"))
                    Communication.Contact($"/service/register.php?hostname={Dns.GetHostName()}", true);
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Could not authenticate");
                Log.Error(LogName, ex);
            }

            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="filePath">The path to the file where the security token is stored</param>
        /// <returns>The decrypted security token</returns>
        private static byte[] GetSecurityToken(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Log.Warn(LogName, $"No token found at {filePath}, this is expected if the client has not authenticated before");
                }
                var token = File.ReadAllBytes(filePath);
                token = DPAPI.UnProtectData(token, true);
                return token;
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Could not get security token");
                Log.Error(LogName, ex);
            }

            return System.Text.Encoding.ASCII.GetBytes("NoToken");
        }

        /// <summary>
        ///     Encrypt and save a security token
        /// </summary>
        /// <param name="filePath">The path to the file where the security token should be stored</param>
        /// <param name="token">The security token to encrypt and save</param>
        private static void SetSecurityToken(string filePath, byte[] token)
        {
            try
            {
                token = DPAPI.ProtectData(token, true);
                File.WriteAllBytes(filePath, token);
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Could not save security token");
                Log.Error(LogName, ex);
            }
        }

        /// <summary>
        ///     Decrypts a response using AES, filtering out encryption flags
        /// </summary>
        /// <param name="toDecode">The string to decrypt</param>
        /// <returns>True if the server was contacted successfully</returns>
        public static string Decrypt(string toDecode)
        {
            // Legacy API support (2 different flags were used at one point)
            const string encryptedFlag = "#!en=";
            const string encryptedFlag2 = "#!enkey=";
            
            if (toDecode.StartsWith(encryptedFlag2))
            {
                var decryptedResponse = toDecode.Substring(encryptedFlag2.Length);
                toDecode = AES.Decrypt(decryptedResponse, Passkey);
                return toDecode;
            }
            if (!toDecode.StartsWith(encryptedFlag)) return toDecode;

            var decrypted = toDecode.Substring(encryptedFlag.Length);
            return AES.Decrypt(decrypted, Passkey);
        }
    }
}
*/