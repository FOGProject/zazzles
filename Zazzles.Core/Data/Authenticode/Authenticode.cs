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
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Zazzles.Core.Data.Authenticode
{
    public static class Authenticode
    {
        private const string OidAuthorityKeyIdentifier = "2.5.29.35";

        /// <summary>
        /// </summary>
        /// <param name="filePath"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <returns>The certificate used to digitally sign a file</returns>
        private static X509Certificate2 ExtractDigitalSignature(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path must be provided!", nameof(filePath));

            var signer = X509Certificate.CreateFromSignedFile(filePath);
            var certificate = new X509Certificate2(signer);
            return certificate;
        }

        /// <summary>
        /// Check if an authenticode on an PE file is both valid and originates from a specified certificate authority
        /// </summary>
        /// <param name="filePath">The location of the PE file</param>
        /// <param name="authority">The certificate authority to validate against</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="CertificateChainNotValidException"></exception>
        /// <exception cref="AuthorityTimeNotValidException"></exception>
        /// <returns>True if the PE file was signed by the CA, and the signature's lifespan is still valid</returns>
        public static bool IsValid(string filePath, X509Certificate2 authority)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path must be provided!", nameof(filePath));
            if (authority == null)
                throw new ArgumentNullException(nameof(authority));

            // Ensure the PE binary was signed by the CA
            // We don't care about it's lifespan yet
            var signingCertificate = ExtractDigitalSignature(filePath);
            var chainValid = IsFromCA(authority, signingCertificate);
            if (!chainValid)
                throw new CertificateChainNotValidException();

            // Since X509VerificationFlags.IgnoreNotTimeValid was set during chain validation during
            // the above steps we must manually validate the time. This is due to limitations in the 
            // .NET X509 api.
            // The authority's validity must be strictly enforced. If the certificate is not yet active
            // or is expired then the PE file's authenticode should automatically be flagged as invalid
            var validationTime = DateTime.Now;
            if (authority.NotBefore >= validationTime || authority.NotAfter <= validationTime)
            {
                throw new AuthorityTimeNotValidException("Certificate authority is no longer valid at current time");
            }

            if (signingCertificate.NotBefore >= validationTime)
            {
                throw new AuthorityTimeNotValidException("Certificate authority is not yet valid at current time");
            }

            var validTimestamp = IsTimestampValid(filePath);

            // If the signing certificate is expired, the authenticode is valid if and only if
            // it also contains a valid timestamp
            if (signingCertificate.NotAfter <= validationTime && !validTimestamp)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///  Validate that certificate came from a specific Certificate Authority
        ///  An X509 Chain validation will occur, ignoring certificate expirations
        ///  and also ignoring any revocations
        ///  TODO: Provide revocation checks for CAs
        ///  TODO: Add support for intermediate CAs
        /// </summary>
        /// <param name="authority">The CA certificate</param>
        /// <param name="certificate">The certificate to validate</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="CertificateChainNotValidException"></exception>
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

            // If the chain validates, check that one of the certificates 
            // in the chain matches the authority's thumbprint
            // this will ensure the authority is present in the X509Chain
            var correctOrigin = chain.ChainElements.Cast<X509ChainElement>()
                .Any(x => x.Certificate.Thumbprint == authority.Thumbprint);
            return correctOrigin;
        }

        /// <summary>
        /// Check if a certificate properly validates using a provided X509Chain
        /// </summary>
        /// <param name="cert">The certificate to validate</param>
        /// <param name="chain">The X509 chain policy used to validate</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="CertificateChainNotValidException"></exception>
        /// <returns>True if the certificate came from the authority</returns>
        private static bool PrettyChainValidation(X509Certificate2 cert, X509Chain chain)
        {
            if (chain == null)
                throw new ArgumentNullException(nameof(chain));

            var isChainValid = chain.Build(cert);
            if (isChainValid) return true;

            var errors = chain.ChainStatus.Select(x => $"{x.StatusInformation.Trim()} ({x.Status})").ToArray();
            var chainErrors = "Unknown errors.";

            if (errors.Length > 0)
                chainErrors = string.Join(", ", errors);

            throw new CertificateChainNotValidException(chainErrors);
        }

        /// <summary>
        /// Check if a PE file has a valid authenticode timestamp
        /// </summary>
        /// <param name="filePath">The path to the PE file to check</param>
        /// <sources>
        /// https://tools.ietf.org/html/rfc5280
        /// https://www.gnutls.org/manual/html_node/X_002e509-extensions.html
        /// http://www.alvestrand.no/objectid/2.5.29.35.html
        /// </sources>
        /// <exception cref="ArgumentException"></exception>
        /// <returns>True if a timestamp is present and is valid (includes revocation checks)</returns>
        public static bool IsTimestampValid(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("Filepath to PE file must be provided!", nameof(filePath));

            // Prevent method from running until a way of getting Mono.Security
            // to run on a mono installation of a different version

            // Use Mono's version of the X509 API to perform authenticode extraction
            // Standard .NET does not yet have the capability to parse this data yet
            // Once Mono parses out the timestamper's certificate, convert it to a native
            // X509Certificate2 to allow the rest of the RSA API to use the native
            // implementation

            throw new NotImplementedException();

            /*
            var deformatter = new AuthenticodeDeformatter(filePath);
            if (deformatter.SigningCertificate == null)
                return false;
            var timestamperCert = new X509Certificate2(deformatter.SigningCertificate.RawData);

            // Gather the needed certificates to validate the timestamper's validity
            var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            //var chainStore = BuildCertChainStore(timestamperCert, store);
            store.Close();

            // Perform a chain validation using the certificate list generated
            // The entire chain should be checked for revocations using offline cache 
            // inorder to speed up the process
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
           // chain.ChainPolicy.ExtraStore.AddRange(chainStore);
            var chainBuilds = false;

            try
            {
                chainBuilds = PrettyChainValidation(timestamperCert, chain);
            } catch (Exception)
            {
                return false;
            }

            if (!chainBuilds)
                return false;

            var peSigner = ExtractDigitalSignature(filePath);
            if (peSigner.NotAfter <= deformatter.Timestamp)
            {
                return false;
            }
            if (peSigner.NotBefore >= deformatter.Timestamp)
            {
                return false;
            }

            return true;
            */
            
        }

        /// <summary>
        /// Extract an X509 extension matching the specified oid
        /// </summary>
        /// <param name="cert"></param>
        /// <param name="oid">The oid to filter by</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns>An asn data wrapper or null if no match was found</returns>
        private static AsnEncodedData ExtractX509Extension(X509Certificate2 cert, string oid)
        {
            if (cert == null)
                throw new ArgumentNullException(nameof(cert));
            if (string.IsNullOrEmpty(oid))
                throw new ArgumentException("Oid must be provided!", nameof(oid));

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
