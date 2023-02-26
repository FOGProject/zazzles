/*
 * Zazzles : A cross platform service framework
 * Copyright (C) 2014-2023 FOG Project
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
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Zazzles.Data;

namespace Zazzles.Middleware
{
    public static class Communication
    {
        private const string LogName = "Middleware::Communication";

        /// <summary>
        ///     Get the parsed response of a server url
        /// </summary>
        /// <param name="postfix">The postfix to attach to the server address</param>
        /// <returns>The parsed response</returns>
        public static Response GetResponse(string postfix)
        {
            try
            {
                var rawResponse = GetRawResponse(postfix);
                var encrypted = rawResponse.StartsWith("#!en");
                if (encrypted)
                    rawResponse = Authentication.Decrypt(rawResponse);

                if (string.IsNullOrEmpty(rawResponse))
                {
                    Log.Error(LogName, "No response recieved");
                    return new Response();
                }

                if (!rawResponse.StartsWith("#!ihc")) return new Response(rawResponse, encrypted);

                return Authentication.HandShake() ? GetResponse(postfix) : new Response();
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Could not contact FOG server");
                Log.Error(LogName, ex);
            }

            return new Response();
        }

        /// <summary>
        ///     Get the parsed response of a server url
        /// </summary>
        /// <param name="postfix">The postfix to attach to the server address</param>
        /// <param name="appendMAC">If the MAC address of the host should be appended to the URL</param>
        /// <returns>The parsed response</returns>
        public static Response GetResponse(string postfix, bool appendMAC)
        {
            if (appendMAC)
                postfix += ((postfix.Contains(".php?") ? "&" : "?") + "mac=" + Configuration.MACAddresses());

            return GetResponse(postfix);
        }

        /// <summary>
        ///     Get the raw response of a server url
        /// </summary>
        /// <param name="postfix">The postfix to attach to the server address</param>
        /// <returns>The unparsed response</returns>
        public static string GetRawResponse(string postfix)
        {
            if (!postfix.Contains("newService"))
            {
                postfix += ((postfix.Contains(".php?") ? "&" : "?") + "newService");
            }
            if (!postfix.Contains("json"))
            {
                postfix += ((postfix.Contains(".php?") ? "&" : "?") + "json");
            }

            Log.Entry(LogName, "URL: " + Configuration.ServerAddress + postfix);

            // Set custom certificate policy manager
            ServicePointManager.ServerCertificateValidationCallback =
                CertificatePolicy.CertValidationCallback;
            // Check and set TLS protocol versions to use for communication to the FOG server
            SetTlsProtocolVersions();

            var webRequest = WebRequest.Create(Configuration.ServerAddress + postfix);

            using (var response = webRequest.GetResponse())
            using (var content = response.GetResponseStream())
            using (var reader = new StreamReader(content))
            {
                var result = reader.ReadToEnd();
                return result;
            }
        }

        /// <summary>
        ///     POST data to a URL
        /// </summary>
        /// <param name="postfix">The text to append to the URL</param>
        /// <param name="param">The params to post</param>
        /// <returns>The response of the server</returns>
        public static Response Post(string postfix, string param)
        {
            Log.Entry(LogName, "POST URL: " + Configuration.ServerAddress + postfix);

            try
            {
                // Set custom certificate policy manager
                ServicePointManager.ServerCertificateValidationCallback =
                    CertificatePolicy.CertValidationCallback;
                // Check and set TLS protocol versions to use for communication to the FOG server
                SetTlsProtocolVersions();

                // Create a request using a URL that can receive a post. 
                var request = (HttpWebRequest)WebRequest.Create(Configuration.ServerAddress + postfix);
                request.Method = "POST";
                request.AllowAutoRedirect = false;

                // Create POST data and convert it to a byte array.
                var byteArray = Encoding.UTF8.GetBytes(param);
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = byteArray.Length;

                // Get the request stream.
                var dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                // Get the response.
                var response = request.GetResponse();
                var httpResponse = (HttpWebResponse)response;
                dataStream = response.GetResponseStream();

                // Open the stream using a StreamReader for easy access.
                var reader = new StreamReader(dataStream);
                var rawResponse = reader.ReadToEnd();
                if (httpResponse.StatusCode == HttpStatusCode.Found)
                {
                    var uri = new Uri(httpResponse.Headers["Location"]);
                    Log.Entry(LogName, "Received HTTP redirect, retrying POST to " + uri.GetLeftPart(UriPartial.Path));
                    if (uri.Scheme.Equals("https"))
                    {
                        Log.Entry(LogName, "This is a HTTPS redirect and so we switch to that in settings file.");
                        Settings.Set("HTTPS", "1", false);
                    }
                    Configuration.ServerAddress = uri.GetLeftPart(UriPartial.Authority) + Settings.Get("WebRoot");
                    return Post(postfix, param);
                }

                // Clean up the streams.
                reader.Close();
                dataStream?.Close();
                response.Close();

                var encrypted = rawResponse.StartsWith("#!en");

                if (encrypted)
                    rawResponse = Authentication.Decrypt(rawResponse);
                return new Response(rawResponse, encrypted);
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Failed to POST data");
                Log.Error(LogName, ex);
            }

            return new Response();
        }

        /// <summary>
        ///     GET a URL but don't check for a response
        /// </summary>
        /// <param name="postfix">The postfix to attach to the server address</param>
        /// <returns>True if the server was contacted successfully</returns>
        public static bool Contact(string postfix)
        {
            try
            {
                GetRawResponse(postfix);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Could not contact FOG server");
                Log.Error(LogName, ex);
            }

            return false;
        }

        /// <summary>
        ///     GET a URL but don't check for a response
        /// </summary>
        /// <param name="postfix">The text to append to the url</param>
        /// <param name="appendMAC">Should the MAC be appended</param>
        /// <returns>True if successful</returns>
        public static bool Contact(string postfix, bool appendMAC)
        {
            if (appendMAC)
                postfix += ((postfix.Contains(".php?") ? "&" : "?") + "mac=" + Configuration.MACAddresses());

            return Contact(postfix);
        }

        /// <summary>
        ///     Downloads a file and creates necessary directories
        /// </summary>
        /// <param name="postfix">The postfix to attach to the server address</param>
        /// <param name="filePath">The location to save the file</param>
        /// <returns>True if the download was successful</returns>
        public static bool DownloadFile(string postfix, string filePath)
        {
            return DownloadExternalFile(Configuration.ServerAddress + postfix, filePath);
        }

        /// <summary>
        ///     Download a file from an external server
        /// </summary>
        /// <param name="url">The URL to download from</param>
        /// <param name="filePath">The path to save the file to</param>
        /// <returns>True if successful</returns>
        public static bool DownloadExternalFile(string url, string filePath)
        {
            Log.Entry(LogName, $"Download: {url}");

            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(filePath))
            {
                Log.Error(LogName, "Invalid parameters");
                return false;
            }
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Could not delete existing file");
                Log.Error(LogName, ex);
                return false;
            }
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                }

                // Set custom certificate policy manager
                ServicePointManager.ServerCertificateValidationCallback =
                    CertificatePolicy.CertValidationCallback;
                // Check and set TLS protocol versions to use for communication to the FOG server
                SetTlsProtocolVersions();

                using (var wclient = new WebClient())
                {
                    wclient.DownloadFile(url, filePath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Could not download file");
                Log.Error(LogName, ex);
                return false;
            }

            return File.Exists(filePath);
        }

        public static void SetTlsProtocolVersions()
        {
            Log.Debug(LogName, "Runtime: " + System.Diagnostics.FileVersionInfo.GetVersionInfo(typeof(int).Assembly.Location).ProductVersion);
            Log.Debug(LogName, "Enabled protocols:   " + ServicePointManager.SecurityProtocol);
            foreach (SecurityProtocolType protocol in Enum.GetValues(typeof(SecurityProtocolType)))
            {
                switch (protocol)
                {
                    case SecurityProtocolType.Tls11:
                    case SecurityProtocolType.Tls12:
                    case (SecurityProtocolType)12288:
                        if (!ServicePointManager.SecurityProtocol.HasFlag(protocol))
                        {
                            Log.Debug(LogName, $"{protocol.ToString()} is NOT enabled. Enabling it now.");
                            ServicePointManager.SecurityProtocol |= protocol;
                        }
                        else
                        {
                            Log.Debug(LogName, $"{protocol.ToString()} is enabled already. Leaving it like this!");
                        }
                        break;
                    case SecurityProtocolType.Ssl3:
                    case SecurityProtocolType.Tls:
                        if (ServicePointManager.SecurityProtocol.HasFlag(protocol))
                        {
                            Log.Debug(LogName, $"{protocol.ToString()} is enabled. Disabling it now.");
                            ServicePointManager.SecurityProtocol &= ~protocol;
                        }
                        else
                        {
                            Log.Debug(LogName, $"{protocol.ToString()} is disabled. Leaving it like this!");
                        }
                        break;
                    case (SecurityProtocolType)0:
                        // Protocol SystemDefault which we just ignore
                        break;
                    default:
                        Log.Debug(LogName,
                            $"Found yet unknown protocol {protocol.ToString()} ({protocol.GetHashCode()}), please add to the code!");
                        break;
                }
            }
            Log.Debug(LogName, "Enabled protocols:   " + ServicePointManager.SecurityProtocol);
        }
    }

    public static class CertificatePolicy
    {
        private const string LogName = "Middleware::Communication";

        public static bool CertValidationCallback(object sender,
            System.Security.Cryptography.X509Certificates.X509Certificate cert,
            System.Security.Cryptography.X509Certificates.X509Chain chain,
            System.Net.Security.SslPolicyErrors polerrors)
        {
            if (polerrors == System.Net.Security.SslPolicyErrors.None)
            {
                return true;
            }
            var certAsString = cert.ToString(true);
            var request = (WebRequest)sender;
            // Allow for the installer to download the CA cert via HTTPS as well
            if (request.RequestUri.AbsolutePath.EndsWith("ca.cert.der") &&
                certAsString.Contains(request.RequestUri.Host) &&
                cert.Issuer.Equals("CN=FOG Server CA"))
            {
                return true;
            }
            // great code from https://stackoverflow.com/a/37657252
            if ((polerrors & System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors) != 0)
            {
                if (chain?.ChainStatus != null)
                {
                    foreach (var status in chain.ChainStatus)
                    {
                        if ((cert.Subject == cert.Issuer) &&
                            (status.Status == X509ChainStatusFlags.UntrustedRoot))
                        {
                            // Self-signed certificates with an untrusted root are fine
                            continue;
                        }
                        if (status.Status != X509ChainStatusFlags.NoError)
                        {
                            if (RSA.IsFromCA(RSA.ServerCertificate(), new X509Certificate2(cert)))
                            {
                                // Ok, Mono is simply tooo dump to use the CA cert from the store
                                // and we need to do this check manually
                                return true;
                            }
                            Log.Entry(LogName, "SSL certificate chain error: " + status.StatusInformation);
                            return false;
                        }
                    }
                }

                // When we get here the only errors in the certificate chain should be
                // untrusted root errors for self-signed certificates. These certificates
                // are valid e.g. for default Exchange server installations.
                return true;
            }
            Log.Entry(LogName, "SSL connection error: " + polerrors);
            return false;
        }
    }
}