/*
 * Zazzles : A cross platform service framework
 * Copyright (C) 2014-2015 FOG Project
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
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Mono.Security.Authenticode;

namespace Zazzles.Data
{
    public static class RSA
    {
        private const string LogName = "Data::RSA";
        const string OID_AUTHORITY_KEY_IDENTIFIER = "2.5.29.35";


        /// <summary>
        ///     Encrypt data using RSA
        /// </summary>
        /// <param name="cert">The X509 certificate to use</param>
        /// <param name="data">The data to encrypt</param>
        /// <returns>A hex string of the encrypted data</returns>
        public static string Encrypt(X509Certificate2 cert, string data)
        {
            if(cert == null)
                throw new ArgumentNullException(nameof(cert));
            if (string.IsNullOrEmpty(data))
                throw new ArgumentException("Data must be provided", nameof(data));

            var byteData = Encoding.UTF8.GetBytes(data);
            var encrypted = Encrypt(cert, byteData);
            return Transform.ByteArrayToHexString(encrypted);
        }

        /// <summary>
        ///     Decrypt data using RSA
        /// </summary>
        /// <param name="cert">The X509 certificate to use</param>
        /// <param name="data">The data to decrypt</param>
        /// <returns>A UTF8 string of the data</returns>
        public static string Decrypt(X509Certificate2 cert, string data)
        {
            if (cert == null)
                throw new ArgumentNullException(nameof(cert));
            if (string.IsNullOrEmpty(data))
                throw new ArgumentException("Data must be provided", nameof(data));

            var byteData = Transform.HexStringToByteArray(data);
            var decrypted = Decrypt(cert, byteData);
            return Encoding.UTF8.GetString(decrypted);
        }

        /// <summary>
        ///     Encrypt data using RSA
        /// </summary>
        /// <param name="cert">The X509 certificate to use</param>
        /// <param name="data">The data to encrypt</param>
        /// <returns>A byte array of the encrypted data</returns>
        public static byte[] Encrypt(X509Certificate2 cert, byte[] data)
        {
            if (cert == null)
                throw new ArgumentNullException(nameof(cert));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var rsa = (RSACryptoServiceProvider) cert?.PublicKey.Key;
            return rsa?.Encrypt(data, false);
        }

        /// <summary>
        ///     Decrypt data using RSA
        /// </summary>
        /// <param name="cert">The X509 certificate to use</param>
        /// <param name="data">The data to decrypt</param>
        /// <returns>A byte array of the decrypted data</returns>
        public static byte[] Decrypt(X509Certificate2 cert, byte[] data)
        {
            if (cert == null)
                throw new ArgumentNullException(nameof(cert));
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (!cert.HasPrivateKey)
                throw new ArgumentException("Certficate must have a private key!", nameof(cert));

            var rsa = (RSACryptoServiceProvider) cert.PrivateKey;
            return rsa.Decrypt(data, false);
        }

        /// <summary>
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>The certificate used to digitally sign a file</returns>
        public static X509Certificate2 ExtractDigitalSignature(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path must be provided!", nameof(filePath));

            try
            {
                var signer = X509Certificate.CreateFromSignedFile(filePath);
                var certificate = new X509Certificate2(signer);
                return certificate;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// </summary>
        /// <returns>The FOG CA root certificate</returns>
        public static X509Certificate2 ServerCertificate()
        {
            return GetRootCertificate("FOG Server CA");
        }

        /// <summary>
        /// </summary>
        /// <returns>The FOG Project root certificate</returns>
        public static X509Certificate2 FOGProjectCertificate()
        {
            return GetRootCertificate("FOG Project");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">The name of the certificate to retrieve</param>
        /// <returns>Returns the first instance of the certificate matching the name</returns>
        public static X509Certificate2 GetRootCertificate(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Certificate name must be provided!", nameof(name));

            try
            {
                X509Certificate2 CAroot = null;
                var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadOnly);
                var cers = store.Certificates.Find(X509FindType.FindBySubjectName, name, true);

                if (cers.Count > 0)
                {
                    Log.Entry(LogName, name + " cert found");
                    CAroot = cers[0];
                }
                store.Close();

                return CAroot;
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Unable to retrieve " + name);
                Log.Error(LogName, ex);
            }

            return null;
        }

        /// <summary>
        ///     Add a CA certificate to the machine store
        /// </summary>
        /// <param name="caCert">The certificate to add</param>
        public static bool InjectCA(X509Certificate2 caCert)
        {
            if (caCert == null)
                throw new ArgumentNullException(nameof(caCert));

            Log.Entry(LogName, "Injecting root CA: " + caCert.FriendlyName);
            try
            {
                var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);
                store.Add(caCert);
                store.Close();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Unable to inject CA");
                Log.Error(LogName, ex);
            }

            return false;
        }

        public static bool IsAuthenticodeValid(string filePath, X509Certificate2 authority)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path must be provided!", nameof(filePath));

            var signingCertificate = ExtractDigitalSignature(filePath);
            var chainValid = IsFromCA(authority, signingCertificate);
            if (!chainValid)
                return false;

            // Since X509VerificationFlags.IgnoreNotTimeValid was set during chain validation,
            // we must manually validate the time. This is due to limitations in the .NET x509 api
            // Strictly check the authority and enforce it's lifespan
            var validationTime = DateTime.Now;
            if (authority.NotBefore >= validationTime || authority.NotAfter <= validationTime)
            {
                Log.Error(LogName, "Certificate validation failed");
                Log.Error(LogName, "Certificate authority is no longer valid at current time");
                return false;
            }

            var validTimestamp = IsTimestampValid(filePath);

            // Under no circumstance is the signing certificate valid
            // if it came from the future.
            if (signingCertificate.NotBefore >= validationTime)
            {
                Log.Error(LogName, "Certificate validation failed");
                Log.Error(LogName, "Certificate was commissioned in the future; Please verify your clock is set correctly");
                return false;
            }

            // Only allow an expired certificate through if the binary also contained a valid timestamp
            if (signingCertificate.NotAfter <= validationTime && !validTimestamp)
            {
                Log.Error(LogName, "Certificate validation failed");
                Log.Error(LogName, "Certificate has expired and the PE file was not timestamped");
                return false;
            }


            return true;
        }

        /// <summary>
        ///     Validate that certificate came from a specific CA
        /// </summary>
        /// <param name="authority">The CA certificate</param>
        /// <param name="certificate">The certificate to validate</param>
        /// <returns>True if the certificate came from the authority</returns>
        public static bool IsFromCA(X509Certificate2 authority, X509Certificate2 certificate)
        {
            if (authority == null)
                throw new ArgumentNullException(nameof(authority));
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));

            var chain = new X509Chain
            {
                ChainPolicy =
                    {
                        RevocationMode = X509RevocationMode.NoCheck,
                        RevocationFlag = X509RevocationFlag.ExcludeRoot,
                        VerificationFlags = X509VerificationFlags.IgnoreNotTimeValid,
                        VerificationTime = DateTime.Now,
                        UrlRetrievalTimeout = new TimeSpan(0, 0, 0)
                    }
            };

            chain.ChainPolicy.ExtraStore.Add(authority);
            var builds = PrettyChainValidation(certificate, chain);
            if (!builds) return false;

            var correctOrigin = chain.ChainElements.Cast<X509ChainElement>().Any(x => x.Certificate.Thumbprint == authority.Thumbprint);
            if (!correctOrigin)
            {
                Log.Error(LogName, "Certificate validation failed");
                Log.Error(LogName, "Trust chain did not complete to the known authority anchor. Thumbprints did not match.");
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Validate that certificate came from a specific CA
        /// </summary>
        /// <returns>True if the certificate came from the authority</returns>
        public static bool PrettyChainValidation(X509Certificate2 cert, X509Chain chain)
        {
            if (chain == null)
                throw new ArgumentNullException(nameof(chain));
            try
            {
                var isChainValid = chain.Build(cert);
                if (isChainValid) return true;

                var errors = chain.ChainStatus.Select(x => $"{x.StatusInformation.Trim()} ({x.Status})").ToArray();
                var chainErrors = "Unknown errors.";

                if (errors.Length > 0)
                    chainErrors = string.Join(", ", errors);

                Log.Error(LogName, "Certificate validation failed");
                Log.Error(LogName, $"Trust chain did not complete to the known authority anchor. Errors: {chainErrors}");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Could not validate X509 chain");
                Log.Error(LogName, ex);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <sources>
        /// https://tools.ietf.org/html/rfc5280
        /// https://www.gnutls.org/manual/html_node/X_002e509-extensions.html
        /// http://www.alvestrand.no/objectid/2.5.29.35.html
        /// </sources>
        /// <returns></returns>
        public static bool IsTimestampValid(string filePath)
        {
            var deformatter = new AuthenticodeDeformatter(filePath);
            var timestampPresent = (deformatter.SigningCertificate != null);
            if (!timestampPresent)
                return false;

            // Convert the mono x509Certificate to the pure .NET version
            // and then generate a cert store in memory of all certificates needed
            // to validate the timestamp
            var signingCert = new X509Certificate2(deformatter.SigningCertificate.RawData);
            var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            var chainStore = BuildCertChainStore(signingCert, store);
            store.Close();

            // Perform 
            var chain = new X509Chain
            {
                ChainPolicy =
                    {
                        RevocationMode = X509RevocationMode.Offline,
                        RevocationFlag = X509RevocationFlag.EntireChain,
                        VerificationTime = DateTime.Now,
                        UrlRetrievalTimeout = new TimeSpan(0, 1, 0)
                    }
            };
            chain.ChainPolicy.ExtraStore.AddRange(chainStore);
            var builds = PrettyChainValidation(signingCert, chain);

            return builds; 
        }

        private static X509Certificate2Collection BuildCertChainStore(X509Certificate2 cert, X509Store store)
        {
            if (cert == null)
                throw new ArgumentNullException(nameof(cert));
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            var collection = new X509Certificate2Collection();
            var authorityKey = ExtractX509Extension(cert, OID_AUTHORITY_KEY_IDENTIFIER);

            var matches = store.Certificates.Find(X509FindType.FindBySubjectKeyIdentifier, authorityKey.RawData, true);
            foreach (var match in matches)
            {
                collection.Add(match);
                collection.AddRange(BuildCertChainStore(match, store));
            }

            return collection;

        }

        private static AsnEncodedData ExtractX509Extension(X509Certificate2 cert, string oid)
        {
            if (cert == null)
                throw new ArgumentNullException(nameof(cert));

            foreach (var extension in cert.Extensions)
            {
                if (extension.Oid.Value != oid) continue;
                var asndata = new AsnEncodedData(extension.Oid, extension.RawData);
                return asndata;
            }

            return null;
        }

    }
}