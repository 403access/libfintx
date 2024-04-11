/*	
 * 	
 *  This file is part of libfintx.
 *  
 *  Copyright (C) 2018 Bjoern Kuensting
 *  
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public
 *  License as published by the Free Software Foundation; either
 *  version 3 of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 *  Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software Foundation,
 *  Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 * 	
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Linq;
using Ionic.Zlib;
using Microsoft.Extensions.Logging;
using libfintx.EBICS.Exceptions;
using libfintx.EBICS.Handler;
using libfintx.EBICS.Responses;
using libfintx.EBICSConfig;
using libfintx.Xml;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;

namespace libfintx.EBICS.Commands
{
    internal abstract class Command
    {
        private static readonly ILogger s_logger = EbicsLogging.CreateLogger<Command>();

        internal Config Config { get; set; }
        internal NamespaceConfig Namespaces { get; set; }

        internal abstract string OrderType { get; }
        internal abstract string OrderAttribute { get; }
        internal abstract TransactionType TransactionType { get; }

        internal abstract IList<XmlDocument> Requests { get; }
        internal abstract XmlDocument InitRequest { get; }
        internal abstract XmlDocument ReceiptRequest { get; }

        protected static string s_signatureAlg => "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
        protected static string s_digestAlg => "http://www.w3.org/2001/04/xmlenc#sha256";

        protected void UpdateResponse(Response resp, DeserializeResponse dr)
        {
            resp.BusinessReturnCode = dr.BusinessReturnCode;
            resp.TechnicalReturnCode = dr.TechnicalReturnCode;
            resp.ReportText = dr.ReportText;
        }

        internal virtual DeserializeResponse Deserialize(string payload)
        {
            using (new MethodLogger(s_logger))
            {
                var doc = XDocument.Parse(payload);
                var xph = new XPathHelper(doc, Namespaces);
                int.TryParse(xph.GetTechReturnCode()?.Value, out var techCode);
                int.TryParse(xph.GetBusReturnCode()?.Value, out var busCode);
                int.TryParse(xph.GetNumSegments()?.Value, out var numSegments);
                int.TryParse(xph.GetSegmentNumber()?.Value, out var segmentNo);
                bool.TryParse(xph.GetSegmentNumber()?.Attribute(XmlNames.lastSegment)?.Value, out var lastSegment);
                Enum.TryParse<TransactionPhase>(xph.GetTransactionPhase()?.Value, out var transPhase);

                var dr = new DeserializeResponse
                {
                    BusinessReturnCode = busCode,
                    TechnicalReturnCode = techCode,
                    NumSegments = numSegments,
                    SegmentNumber = segmentNo,
                    LastSegment = lastSegment,
                    TransactionId = xph.GetTransactionID()?.Value,
                    Phase = transPhase,
                    ReportText = xph.GetReportText()?.Value
                };

                s_logger.LogDebug("DeserializeResponse: {response}", dr);
                return dr;
            }
        }

        protected byte[] DecryptOrderData(XPathHelper xph)
        {
            using (new MethodLogger(s_logger))
            {
                var encryptedOd = Convert.FromBase64String(xph.GetOrderData()?.Value);

                if (!Enum.TryParse<CryptVersion>(xph.GetEncryptionPubKeyDigestVersion()?.Value,
                    out var transKeyEncVersion))
                {
                    throw new DeserializationException(
                        string.Format("Encryption version {0} not supported",
                            xph.GetEncryptionPubKeyDigestVersion()?.Value), xph.Xml);
                }

                var encryptionPubKeyDigest = Convert.FromBase64String(xph.GetEncryptionPubKeyDigest()?.Value);
                var encryptedTransKey = Convert.FromBase64String(xph.GetTransactionKey()?.Value);

                var transKey = DecryptRsa(encryptedTransKey);
                var decryptedOd = DecryptAES(encryptedOd, transKey);

                if (!StructuralComparisons.StructuralEqualityComparer.Equals(Config.User.CryptKeys.Digest,
                    encryptionPubKeyDigest))
                {
                    throw new DeserializationException("Wrong digest in xml", xph.Xml);
                }

                return decryptedOd;
            }
        }

        protected DateTime ParseTimestamp(string dt)
        {
            DateTime.TryParseExact(dt, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var parsed);
            return parsed;
        }

        protected byte[] DecryptRsa(byte[] ciphertext)
        {
            using (new MethodLogger(s_logger))
            {
                var rsa = Config.User.CryptKeys.PrivateKey;
                return rsa.Decrypt(ciphertext, RSAEncryptionPadding.Pkcs1);
            }
        }

        protected byte[] EncryptRsa(byte[] ciphertext)
        {
            using (new MethodLogger(s_logger))
            {
                var rsa = Config.Bank.CryptKeys.PublicKey;
                return rsa.Encrypt(ciphertext, RSAEncryptionPadding.Pkcs1);
            }
        }

        protected byte[] EncryptAes(byte[] ciphertext, byte[] transactionKey)
        {
            using (new MethodLogger(s_logger))
            {
                using (var aesAlg = Aes.Create())
                {
                    aesAlg.Mode = CipherMode.CBC;
                    aesAlg.Padding = PaddingMode.ANSIX923;
                    aesAlg.IV = new byte[16];
                    aesAlg.Key = transactionKey;
                    try
                    {
                        using (var encryptor = aesAlg.CreateEncryptor())
                        {
                            return encryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                        }
                    }
                    catch (CryptographicException e)
                    {
                        aesAlg.Padding = PaddingMode.ISO10126;
                        using (var encryptor = aesAlg.CreateEncryptor())
                        {
                            return encryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                        }
                    }
                }
            }
        }

        protected IList<string> Segment(string b64text)
        {
            using (new MethodLogger(s_logger))
            {
                var k = b64text.Length;
                var i = 0;

                var ret = new List<string>();

                while (true)
                {
                    if (k > 1024)
                    {
                        var partialStr = b64text.Substring(i, 1024);
                        ret.Add(partialStr);
                        i += 1024;
                        k -= 1024;
                    }
                    else
                    {
                        var partialStr = b64text.Substring(i);
                        ret.Add(partialStr);
                        break;
                    }
                }

                return ret;
            }
        }

        protected byte[] DecryptAES(byte[] ciphertext, byte[] transactionKey)
        {
            using (new MethodLogger(s_logger))
            {
                using (var aesAlg = Aes.Create())
                {
                    aesAlg.Mode = CipherMode.CBC;
                    aesAlg.Padding = PaddingMode.ANSIX923;
                    aesAlg.IV = new byte[16];
                    aesAlg.Key = transactionKey;
                    try
                    {
                        using (var decryptor = aesAlg.CreateDecryptor())
                        {
                            return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                        }
                    }
                    catch (CryptographicException e)
                    {
                        aesAlg.Padding = PaddingMode.ISO10126;
                        using (var decryptor = aesAlg.CreateDecryptor())
                        {
                            return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                        }
                    }
                }
            }
        }

        protected byte[] Decompress(byte[] buffer)
        {
            using (new MethodLogger(s_logger))
            {
                using (var output = new MemoryStream())
                {
                    using (var zs = new ZlibStream(output, CompressionMode.Decompress))
                    {
                        zs.Write(buffer, 0, buffer.Length);
                    }

                    return output.ToArray();
                }
            }
        }

        protected byte[] Compress(byte[] buffer)
        {
            using (new MethodLogger(s_logger))
            {
                using (var output = new MemoryStream())
                {
                    using (var zs = new ZlibStream(output, CompressionMode.Compress))
                    {
                        zs.Write(buffer, 0, buffer.Length);
                    }

                    return output.ToArray();
                }
            }
        }

        protected XmlDocument AuthenticateXml(XmlDocument doc, string referenceUri,
            IDictionary<string, string> cnm)
        {
            using (new MethodLogger(s_logger))
            {
                doc.PreserveWhitespace = true;
                var sigDoc = new CustomSignedXml(doc)
                {
                    SignatureKey = Config.User.AuthKeys.PrivateKey,
                    SignaturePadding = RSASignaturePadding.Pkcs1,
                    CanonicalizationAlgorithm = SignedXml.XmlDsigC14NTransformUrl,
                    SignatureAlgorithm = s_signatureAlg,
                    DigestAlgorithm = s_digestAlg,
                    ReferenceUri = referenceUri ?? CustomSignedXml.DefaultReferenceUri
                };

                var nm = new XmlNamespaceManager(doc.NameTable);
                nm.AddNamespace(Namespaces.EbicsPrefix, Namespaces.Ebics);
                if (cnm != null && cnm.Count > 0)
                {
                    foreach (var kv in cnm)
                    {
                        nm.AddNamespace(kv.Key, kv.Value);
                    }
                }

                sigDoc.NamespaceManager = nm;

                sigDoc.ComputeSignature();

                var xmlDigitalSignature = sigDoc.GetXml();
                var headerNode = doc.SelectSingleNode($"//{Namespaces.EbicsPrefix}:{XmlNames.AuthSignature}", nm);
                foreach (XmlNode child in xmlDigitalSignature.ChildNodes)
                {
                    headerNode.AppendChild(headerNode.OwnerDocument.ImportNode(child, true));
                }

                return doc;
            }
        }
        
        // Der Nachrichteninhalt ist semantisch nicht EBICS-konform.
        // Übertragung wird abgebrochen. EBICS-Returncode='[EBICS_INVALID_REQUEST_CONTENT] Message content semantically not compliant to EBICS',
        // Fehlerbeschreibung='de.ppi.tcu.ebics.base.exceptions.InvalidEncryptionDataException: de.ppi.fis.travic.ebics.common.exceptions.InvalidCryptoDataException: javax.crypto.BadPaddingException: Invalid PKCS#1 padding: encrypted message and modulus lengths do not match!'
        public static byte[] rsaSignPss(string rsaPrivateKey, byte[] data, int dwKeySize=2048)
        {
            RSACryptoServiceProvider RSAalg = new RSACryptoServiceProvider(dwKeySize);
            RSAalg.PersistKeyInCsp = false;
            RSAalg.FromXmlString(rsaPrivateKey);
            RSAParameters rsaParams = RSAalg.ExportParameters(true);
            RSA RSACng = RSA.Create();
            RSACng.ImportParameters(rsaParams);
            byte[] signature = RSACng.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            return signature;
        }

        private static bool rsaVerifySignaturePss(string rsaPublicKey, byte[] dataToSign, byte[] signature)
        {
            try
            {
                RSACryptoServiceProvider RSAalg = new RSACryptoServiceProvider(2048);
                RSAalg.PersistKeyInCsp = false;
                RSAalg.FromXmlString(rsaPublicKey);
                RSAParameters rsaParams = RSAalg.ExportParameters(false);
                RSA RSACng = RSA.Create();
                RSACng.ImportParameters(rsaParams);
                return RSACng.VerifyData(dataToSign, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            }
            catch (CryptographicException e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        protected byte[] SignData(byte[] data, SignKeyPair kp)
        {
            if (kp.Version == SignVersion.A005)
            {
                return kp.PrivateKey.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            else if (kp.Version == SignVersion.A006)
            {
                //string privateRsaKeyXml = kp.PrivateKey.ToXmlString(true);
                //string publicRsaKeyXml = kp.PublicKey.ToXmlString(false);
                //byte[] signature = rsaSignPss(privateRsaKeyXml, data);
                //bool signatureVerified = rsaVerifySignaturePss(publicRsaKeyXml, data, signature);
                //if (signatureVerified)
                //{
                //    return signature;
                //}
                //else
                //{
                //    throw new CryptographicException($"Die Signatur konnte nicht verifiziert werden");
                //}
                return kp.PrivateKey.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            }
            else
                throw new CryptographicException($"Derzeit wird nur die Signaturversion {SignVersion.A005} und {SignVersion.A006} unterstützt");

        }

        public override string ToString()
        {
            return
                $"{nameof(OrderType)}: {OrderType}, {nameof(OrderAttribute)}: {OrderAttribute}, {nameof(TransactionType)}: {TransactionType}";
        }
    }
}
