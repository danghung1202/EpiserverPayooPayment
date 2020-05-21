using System;
using System.IO;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web.Hosting;

namespace Foundation.Commerce.Payment.Payoo
{
    public class CmsCryptography
    {
        private X509Certificate2 _SignerCert, _RecipientCert;
        private Encoding _Enc = Encoding.UTF8;

        public CmsCryptography()
        {
            LoadSignerCredential(Path.Combine(HostingEnvironment.ApplicationPhysicalPath, @"App_Data\Certificates\biz_pkcs12.p12"), "biz");
            RecipientPublicCertPath = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, @"App_Data\Certificates\payoo_public_cert_sandbox.pem");
        }

        /// <summary>
        /// The RecipientPublicCertPath property sets recipient public certificate path.
        /// </summary>
        public string RecipientPublicCertPath
        {
            set
            {
                _RecipientCert = new X509Certificate2(value);
            }
        }

        #region Public Methods

        /// <summary>
        /// Load signer credential.
        /// </summary>
        public void LoadSignerCredential(string signerCertPath, string signerCertPassword)
        {
            try
            {
                _SignerCert = new X509Certificate2(signerCertPath, signerCertPassword, X509KeyStorageFlags.MachineKeySet
                                                                                      | X509KeyStorageFlags.Exportable);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Load signer credential.
        /// </summary>
        public void LoadSignerCredential(byte[] rawData, string signerCertPassword)
        {
            try
            {
                _SignerCert = new X509Certificate2(rawData, signerCertPassword, X509KeyStorageFlags.MachineKeySet
                                                                              | X509KeyStorageFlags.Exportable);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Load recipient public certificate.
        /// </summary>
        public void LoadRecipientPublicCert(byte[] rawData)
        {
            try
            {
                _RecipientCert = new X509Certificate2(rawData);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Creates a signature to the CMS/PKCS #7 message. 
        /// </summary>
        public string Sign(string TextData)
        {
            try
            {
                byte[] signedBytes = ComputeSignature(_Enc.GetBytes(TextData));
                return Convert.ToBase64String(signedBytes);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Creates a signature to the CMS/PKCS #7 message. 
        /// </summary>
        public string Sign(byte[] Data)
        {
            try
            {
                byte[] signedBytes = ComputeSignature(Data);
                return Convert.ToBase64String(signedBytes);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Creates a signature and encrypts the contents of the CMS/PKCS #7 message.
        /// </summary>
        public string SignAndEncrypt(string TextData)
        {
            try
            {
                byte[] signedBytes = ComputeSignatureWithNotDetach(_Enc.GetBytes(TextData));
                return Convert.ToBase64String(Encrypt(signedBytes));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Creates a signature and encrypts the contents of the CMS/PKCS #7 message.
        /// </summary>
        public string SignAndEncrypt(byte[] Data)
        {
            try
            {
                byte[] signedBytes = ComputeSignatureWithNotDetach(Data);
                return Convert.ToBase64String(Encrypt(signedBytes));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Verifies the digital signatures on the signed CMS/PKCS #7 message.
        /// </summary>
        public bool Verify(string TextData, string DigitalSignature)
        {
            try
            {
                byte[] encodeMessage = Convert.FromBase64String(DigitalSignature);
                return VerifySignature(_Enc.GetBytes(TextData), encodeMessage);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Verifies the digital signatures on the signed CMS/PKCS #7 message.
        /// </summary>
        public bool Verify(byte[] Data, string DigitalSignature)
        {
            try
            {
                byte[] encodeMessage = Convert.FromBase64String(DigitalSignature);
                return VerifySignature(Data, encodeMessage);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Encrypts the contents of the CMS/PKCS #7 message.
        /// </summary>
        public byte[] Encrypt(byte[] Data)
        {
            try
            {
                ContentInfo cont = new ContentInfo(Data);
                EnvelopedCms env = new EnvelopedCms(cont);
                CmsRecipient recipient = new CmsRecipient(SubjectIdentifierType.IssuerAndSerialNumber, _RecipientCert);
                env.Encrypt(recipient);
                return env.Encode();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region Private Methods

        private bool VerifySignature(byte[] messageBytes, byte[] encodeMessage)
        {
            try
            {
                ContentInfo cont = new ContentInfo(messageBytes);
                SignedCms verified = new SignedCms(cont, true);
                verified.Decode(encodeMessage);
                X509Certificate2Collection Collection =
                new X509Certificate2Collection(_RecipientCert);
                try
                {
                    verified.CheckSignature(Collection, true);
                    if (verified.Certificates.Count > 0)
                    {
                        if (!verified.Certificates[0].Equals(_RecipientCert))
                        {
                            return false;
                        }
                    }
                }
                catch (System.Security.Cryptography.CryptographicException ex)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private byte[] ComputeSignature(byte[] messageBytes)
        {
            try
            {
                ContentInfo cont = new ContentInfo(messageBytes);
                SignedCms signed = new SignedCms(cont, true);
                CmsSigner signer = new CmsSigner(_SignerCert);
                signer.IncludeOption = X509IncludeOption.None;
                signed.ComputeSignature(signer);
                return signed.Encode();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private byte[] VerifySignatureWithNotDetach(byte[] encodeMessage)
        {
            try
            {
                SignedCms verified = new SignedCms();
                verified.Decode(encodeMessage);
                try
                {
                    verified.CheckSignature(true);
                }
                catch (System.Security.Cryptography.CryptographicException ex)
                {
                    return null;
                }
                return verified.ContentInfo.Content;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private byte[] ComputeSignatureWithNotDetach(byte[] messageBytes)
        {
            try
            {
                ContentInfo content = new ContentInfo(messageBytes);
                SignedCms signed = new SignedCms(content);
                CmsSigner signer = new CmsSigner(_SignerCert);
                signed.ComputeSignature(signer);
                return signed.Encode();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

    }
}
