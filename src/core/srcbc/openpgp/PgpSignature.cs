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

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Utilities.Date;

namespace Org.BouncyCastle.Bcpg.OpenPgp
{
	/// <remarks>A PGP signature object.</remarks>
    public class PgpSignature
    {
        public const int BinaryDocument = 0x00;
        public const int CanonicalTextDocument = 0x01;
        public const int StandAlone = 0x02;

        public const int DefaultCertification = 0x10;
        public const int NoCertification = 0x11;
        public const int CasualCertification = 0x12;
        public const int PositiveCertification = 0x13;

        public const int SubkeyBinding = 0x18;
		public const int PrimaryKeyBinding = 0x19;
		public const int DirectKey = 0x1f;
        public const int KeyRevocation = 0x20;
        public const int SubkeyRevocation = 0x28;
        public const int CertificationRevocation = 0x30;
        public const int Timestamp = 0x40;

        private readonly SignaturePacket	sigPck;
        private readonly int				signatureType;
        private readonly TrustPacket		trustPck;

		private ISigner	sig;
		private byte	lastb; // Initial value anything but '\r'

		internal PgpSignature(
            BcpgInputStream bcpgInput)
            : this((SignaturePacket)bcpgInput.ReadPacket())
        {
        }

		internal PgpSignature(
            SignaturePacket sigPacket)
			: this(sigPacket, null)
        {
        }

        internal PgpSignature(
            SignaturePacket	sigPacket,
            TrustPacket		trustPacket)
        {
			if (sigPacket == null)
				throw new ArgumentNullException("sigPacket");

			this.sigPck = sigPacket;
			this.signatureType = sigPck.SignatureType;
			this.trustPck = trustPacket;
        }

		private void GetSig()
        {
            this.sig = SignerUtilities.GetSigner(
				PgpUtilities.GetSignatureName(sigPck.KeyAlgorithm, sigPck.HashAlgorithm));
        }

		/// <summary>The OpenPGP version number for this signature.</summary>
		public int Version
		{
			get { return sigPck.Version; }
		}

		/// <summary>The key algorithm associated with this signature.</summary>
		public PublicKeyAlgorithmTag KeyAlgorithm
		{
			get { return sigPck.KeyAlgorithm; }
		}

		/// <summary>The hash algorithm associated with this signature.</summary>
		public HashAlgorithmTag HashAlgorithm
		{
			get { return sigPck.HashAlgorithm; }
		}

		public void InitVerify(
            PgpPublicKey pubKey)
        {
			lastb = 0;
            if (sig == null)
            {
                GetSig();
            }
            try
            {
                sig.Init(false, pubKey.GetKey());
            }
            catch (InvalidKeyException e)
            {
                throw new PgpException("invalid key.", e);
            }
        }

        public void Update(
            byte b)
        {
            if (signatureType == CanonicalTextDocument)
            {
				doCanonicalUpdateByte(b);
            }
            else
            {
                sig.Update(b);
            }
        }

		private void doCanonicalUpdateByte(
			byte b)
		{
			if (b == '\r')
			{
				doUpdateCRLF();
			}
			else if (b == '\n')
			{
				if (lastb != '\r')
				{
					doUpdateCRLF();
				}
			}
			else
			{
				sig.Update(b);
			}

			lastb = b;
		}

		private void doUpdateCRLF()
		{
			sig.Update((byte)'\r');
			sig.Update((byte)'\n');
		}

		public void Update(
            params byte[] bytes)
        {
			Update(bytes, 0, bytes.Length);
        }

		public void Update(
            byte[]	bytes,
            int		off,
            int		length)
        {
            if (signatureType == CanonicalTextDocument)
            {
                int finish = off + length;

				for (int i = off; i != finish; i++)
                {
                    doCanonicalUpdateByte(bytes[i]);
                }
            }
            else
            {
                sig.BlockUpdate(bytes, off, length);
            }
        }

		public bool Verify()
        {
            byte[] trailer = GetSignatureTrailer();
            sig.BlockUpdate(trailer, 0, trailer.Length);

			return sig.VerifySignature(GetSignature());
        }

		private void UpdateWithIdData(
			int		header,
			byte[]	idBytes)
		{
			this.Update(
				(byte) header,
				(byte)(idBytes.Length >> 24),
				(byte)(idBytes.Length >> 16),
				(byte)(idBytes.Length >> 8),
				(byte)(idBytes.Length));
			this.Update(idBytes);
		}

		private void UpdateWithPublicKey(
			PgpPublicKey key)
		{
			byte[] keyBytes = GetEncodedPublicKey(key);

			this.Update(
				(byte) 0x99,
				(byte)(keyBytes.Length >> 8),
				(byte)(keyBytes.Length));
			this.Update(keyBytes);
		}

		/// <summary>
		/// Verify the signature as certifying the passed in public key as associated
		/// with the passed in user attributes.
		/// </summary>
		/// <param name="userAttributes">User attributes the key was stored under.</param>
		/// <param name="key">The key to be verified.</param>
		/// <returns>True, if the signature matches, false otherwise.</returns>
		public bool VerifyCertification(
			PgpUserAttributeSubpacketVector	userAttributes,
			PgpPublicKey					key)
		{
			UpdateWithPublicKey(key);

			//
			// hash in the userAttributes
			//
			try
			{
				MemoryStream bOut = new MemoryStream();
				foreach (UserAttributeSubpacket packet in userAttributes.ToSubpacketArray())
				{
					packet.Encode(bOut);
				}
				UpdateWithIdData(0xd1, bOut.ToArray());
			}
			catch (IOException e)
			{
				throw new PgpException("cannot encode subpacket array", e);
			}

			this.Update(sigPck.GetSignatureTrailer());

			return sig.VerifySignature(this.GetSignature());
		}

		/// <summary>
		/// Verify the signature as certifying the passed in public key as associated
		/// with the passed in ID.
		/// </summary>
		/// <param name="id">ID the key was stored under.</param>
		/// <param name="key">The key to be verified.</param>
		/// <returns>True, if the signature matches, false otherwise.</returns>
        public bool VerifyCertification(
            string			id,
            PgpPublicKey	key)
        {
			UpdateWithPublicKey(key);

			//
            // hash in the id
            //
            UpdateWithIdData(0xb4, Strings.ToUtf8ByteArray(id));

			Update(sigPck.GetSignatureTrailer());

			return sig.VerifySignature(GetSignature());
        }

		/// <summary>Verify a certification for the passed in key against the passed in master key.</summary>
		/// <param name="masterKey">The key we are verifying against.</param>
		/// <param name="pubKey">The key we are verifying.</param>
		/// <returns>True, if the certification is valid, false otherwise.</returns>
        public bool VerifyCertification(
            PgpPublicKey	masterKey,
            PgpPublicKey	pubKey)
        {
			UpdateWithPublicKey(masterKey);
			UpdateWithPublicKey(pubKey);

			Update(sigPck.GetSignatureTrailer());

			return sig.VerifySignature(GetSignature());
        }

		/// <summary>Verify a key certification, such as revocation, for the passed in key.</summary>
		/// <param name="pubKey">The key we are checking.</param>
		/// <returns>True, if the certification is valid, false otherwise.</returns>
        public bool VerifyCertification(
            PgpPublicKey pubKey)
        {
            if (SignatureType != KeyRevocation
                && SignatureType != SubkeyRevocation)
            {
                throw new InvalidOperationException("signature is not a key signature");
            }

			UpdateWithPublicKey(pubKey);

            Update(sigPck.GetSignatureTrailer());

			return sig.VerifySignature(GetSignature());
        }

		public int SignatureType
        {
			get { return sigPck.SignatureType; }
        }

		/// <summary>The ID of the key that created the signature.</summary>
        public long KeyId
        {
            get { return sigPck.KeyId; }
        }

		[Obsolete("Use 'CreationTime' property instead")]
		public DateTime GetCreationTime()
		{
			return CreationTime;
		}

		/// <summary>The creation time of this signature.</summary>
        public DateTime CreationTime
        {
			get { return DateTimeUtilities.UnixMsToDateTime(sigPck.CreationTime); }
        }

		public byte[] GetSignatureTrailer()
        {
            return sigPck.GetSignatureTrailer();
        }

		/// <summary>
		/// Return true if the signature has either hashed or unhashed subpackets.
		/// </summary>
		public bool HasSubpackets
		{
			get
			{
				return sigPck.GetHashedSubPackets() != null
					|| sigPck.GetUnhashedSubPackets() != null;
			}
		}

		public PgpSignatureSubpacketVector GetHashedSubPackets()
        {
            return createSubpacketVector(sigPck.GetHashedSubPackets());
        }

		public PgpSignatureSubpacketVector GetUnhashedSubPackets()
        {
            return createSubpacketVector(sigPck.GetUnhashedSubPackets());
        }

		private PgpSignatureSubpacketVector createSubpacketVector(SignatureSubpacket[] pcks)
		{
			return pcks == null ? null : new PgpSignatureSubpacketVector(pcks);
		}

		public byte[] GetSignature()
        {
            MPInteger[] sigValues = sigPck.GetSignature();
            byte[] signature;

			if (sigValues != null)
			{
				if (sigValues.Length == 1)    // an RSA signature
				{
					signature = sigValues[0].Value.ToByteArrayUnsigned();
				}
				else
				{
					try
					{
						signature = new DerSequence(
							new DerInteger(sigValues[0].Value),
							new DerInteger(sigValues[1].Value)).GetEncoded();
					}
					catch (IOException e)
					{
						throw new PgpException("exception encoding DSA sig.", e);
					}
				}
			}
			else
			{
				signature = sigPck.GetSignatureBytes();
			}

			return signature;
        }

		// TODO Handle the encoding stuff by subclassing BcpgObject?
		public byte[] GetEncoded()
        {
            MemoryStream bOut = new MemoryStream();

			Encode(bOut);

			return bOut.ToArray();
        }

		public void Encode(
            Stream outStream)
        {
            BcpgOutputStream bcpgOut = BcpgOutputStream.Wrap(outStream);

			bcpgOut.WritePacket(sigPck);

			if (trustPck != null)
            {
                bcpgOut.WritePacket(trustPck);
            }
        }

		private byte[] GetEncodedPublicKey(
			PgpPublicKey pubKey) 
		{
			try
			{
				return pubKey.publicPk.GetEncodedContents();
			}
			catch (IOException e)
			{
				throw new PgpException("exception preparing key.", e);
			}
		}
    }
}
