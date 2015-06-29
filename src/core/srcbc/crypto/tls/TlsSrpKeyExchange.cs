/*
 * $Id$
 *
 * This file is part of the iText (R) project.
 * Copyright (c) 1998-2015 iText Group NV
 * Authors: Bruno Lowagie, Paulo Soares, et al.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License version 3
 * as published by the Free Software Foundation with the addition of the
 * following permission added to Section 15 as permitted in Section 7(a):
 * FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
 * ITEXT GROUP. ITEXT GROUP DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
 * OF THIRD PARTY RIGHTS
 *
 * This program is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 * or FITNESS FOR A PARTICULAR PURPOSE.
 * See the GNU Affero General Public License for more details.
 * You should have received a copy of the GNU Affero General Public License
 * along with this program; if not, see http://www.gnu.org/licenses or write to
 * the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
 * Boston, MA, 02110-1301 USA, or download the license from the following URL:
 * http://itextpdf.com/terms-of-use/
 *
 * The interactive user interfaces in modified source and object code versions
 * of this program must display Appropriate Legal Notices, as required under
 * Section 5 of the GNU Affero General Public License.
 *
 * In accordance with Section 7(b) of the GNU Affero General Public License,
 * a covered work must retain the producer line in every PDF that is created
 * or manipulated using iText.
 *
 * You can be released from the requirements of the license by purchasing
 * a commercial license. Buying such a license is mandatory as soon as you
 * develop commercial activities involving the iText software without
 * disclosing the source code of your own applications.
 * These activities include: offering paid services to customers as an ASP,
 * serving PDFs on the fly in a web application, shipping iText with a closed
 * source product.
 *
 * For more information, please contact iText Software Corp. at this
 * address: sales@itextpdf.com
 */

using System;
using System.IO;

using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Agreement.Srp;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.IO;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;

namespace Org.BouncyCastle.Crypto.Tls
{
    /// <summary>
    /// TLS 1.1 SRP key exchange.
    /// </summary>
    internal class TlsSrpKeyExchange
        : TlsKeyExchange
    {
        protected TlsClientContext context;
        protected KeyExchangeAlgorithm keyExchange;
        protected TlsSigner tlsSigner;
        protected byte[] identity;
        protected byte[] password;

        protected AsymmetricKeyParameter serverPublicKey = null;

        protected byte[] s = null;
        protected BigInteger B = null;
        protected Srp6Client srpClient = new Srp6Client();

        internal TlsSrpKeyExchange(TlsClientContext context, KeyExchangeAlgorithm keyExchange,
            byte[] identity, byte[] password)
        {
            switch (keyExchange)
            {
                case KeyExchangeAlgorithm.SRP:
                    this.tlsSigner = null;
                    break;
                case KeyExchangeAlgorithm.SRP_RSA:
                    this.tlsSigner = new TlsRsaSigner();
                    break;
                case KeyExchangeAlgorithm.SRP_DSS:
                    this.tlsSigner = new TlsDssSigner();
                    break;
                default:
                    throw new ArgumentException("unsupported key exchange algorithm", "keyExchange");
            }

            this.context = context;
            this.keyExchange = keyExchange;
            this.identity = identity;
            this.password = password;
        }

        public virtual void SkipServerCertificate()
        {
            if (tlsSigner != null)
            {
                throw new TlsFatalAlert(AlertDescription.unexpected_message);
            }
        }

        public virtual void ProcessServerCertificate(Certificate serverCertificate)
        {
            if (tlsSigner == null)
            {
                throw new TlsFatalAlert(AlertDescription.unexpected_message);
            }

            X509CertificateStructure x509Cert = serverCertificate.certs[0];
            SubjectPublicKeyInfo keyInfo = x509Cert.SubjectPublicKeyInfo;

            try
            {
                this.serverPublicKey = PublicKeyFactory.CreateKey(keyInfo);
            }
//			catch (RuntimeException)
            catch (Exception)
            {
                throw new TlsFatalAlert(AlertDescription.unsupported_certificate);
            }

            if (!tlsSigner.IsValidPublicKey(this.serverPublicKey))
            {
                throw new TlsFatalAlert(AlertDescription.certificate_unknown);
            }

            TlsUtilities.ValidateKeyUsage(x509Cert, KeyUsage.DigitalSignature);

            // TODO
            /*
            * Perform various checks per RFC2246 7.4.2: "Unless otherwise specified, the
            * signing algorithm for the certificate must be the same as the algorithm for the
            * certificate key."
            */
        }

        public virtual void SkipServerKeyExchange()
        {
            throw new TlsFatalAlert(AlertDescription.unexpected_message);
        }

        public virtual void ProcessServerKeyExchange(Stream input)
        {
            SecurityParameters securityParameters = context.SecurityParameters;

            Stream sigIn = input;
            ISigner signer = null;

            if (tlsSigner != null)
            {
                signer = InitSigner(tlsSigner, securityParameters);
                sigIn = new SignerStream(input, signer, null);
            }

            byte[] NBytes = TlsUtilities.ReadOpaque16(sigIn);
            byte[] gBytes = TlsUtilities.ReadOpaque16(sigIn);
            byte[] sBytes = TlsUtilities.ReadOpaque8(sigIn);
            byte[] BBytes = TlsUtilities.ReadOpaque16(sigIn);

            if (signer != null)
            {
                byte[] sigByte = TlsUtilities.ReadOpaque16(input);

                if (!signer.VerifySignature(sigByte))
                {
                    throw new TlsFatalAlert(AlertDescription.decrypt_error);
                }
            }

            BigInteger N = new BigInteger(1, NBytes);
            BigInteger g = new BigInteger(1, gBytes);

            // TODO Validate group parameters (see RFC 5054)
            //throw new TlsFatalAlert(AlertDescription.insufficient_security);

            this.s = sBytes;

            /*
            * RFC 5054 2.5.3: The client MUST abort the handshake with an "illegal_parameter"
            * alert if B % N = 0.
            */
            try
            {
                this.B = Srp6Utilities.ValidatePublicValue(N, new BigInteger(1, BBytes));
            }
            catch (CryptoException)
            {
                throw new TlsFatalAlert(AlertDescription.illegal_parameter);
            }

            this.srpClient.Init(N, g, new Sha1Digest(), context.SecureRandom);
        }

        public virtual void ValidateCertificateRequest(CertificateRequest certificateRequest)
        {
            throw new TlsFatalAlert(AlertDescription.unexpected_message);
        }

        public virtual void SkipClientCredentials()
        {
            // OK
        }
        
        public virtual void ProcessClientCredentials(TlsCredentials clientCredentials)
        {
            throw new TlsFatalAlert(AlertDescription.internal_error);
        }

        public virtual void GenerateClientKeyExchange(Stream output)
        {
            byte[] keData = BigIntegers.AsUnsignedByteArray(srpClient.GenerateClientCredentials(s,
                this.identity, this.password));
            TlsUtilities.WriteOpaque16(keData, output);
        }

        public virtual byte[] GeneratePremasterSecret()
        {
            try
            {
                // TODO Check if this needs to be a fixed size
                return BigIntegers.AsUnsignedByteArray(srpClient.CalculateSecret(B));
            }
            catch (CryptoException)
            {
                throw new TlsFatalAlert(AlertDescription.illegal_parameter);
            }
        }

        protected virtual ISigner InitSigner(TlsSigner tlsSigner, SecurityParameters securityParameters)
        {
            ISigner signer = tlsSigner.CreateVerifyer(this.serverPublicKey);
            signer.BlockUpdate(securityParameters.clientRandom, 0, securityParameters.clientRandom.Length);
            signer.BlockUpdate(securityParameters.serverRandom, 0, securityParameters.serverRandom.Length);
            return signer;
        }
    }
}
