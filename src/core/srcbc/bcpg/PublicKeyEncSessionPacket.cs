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

using Org.BouncyCastle.Math;

namespace Org.BouncyCastle.Bcpg
{
	/// <remarks>Basic packet for a PGP public key.</remarks>
	public class PublicKeyEncSessionPacket
		: ContainedPacket //, PublicKeyAlgorithmTag
	{
		private int version;
		private long keyId;
		private PublicKeyAlgorithmTag algorithm;
		private BigInteger[] data;

		internal PublicKeyEncSessionPacket(
			BcpgInputStream bcpgIn)
		{
			version = bcpgIn.ReadByte();

			keyId |= (long)bcpgIn.ReadByte() << 56;
			keyId |= (long)bcpgIn.ReadByte() << 48;
			keyId |= (long)bcpgIn.ReadByte() << 40;
			keyId |= (long)bcpgIn.ReadByte() << 32;
			keyId |= (long)bcpgIn.ReadByte() << 24;
			keyId |= (long)bcpgIn.ReadByte() << 16;
			keyId |= (long)bcpgIn.ReadByte() << 8;
			keyId |= (uint)bcpgIn.ReadByte();

			algorithm = (PublicKeyAlgorithmTag) bcpgIn.ReadByte();

			switch ((PublicKeyAlgorithmTag) algorithm)
			{
				case PublicKeyAlgorithmTag.RsaEncrypt:
				case PublicKeyAlgorithmTag.RsaGeneral:
					data = new BigInteger[]{ new MPInteger(bcpgIn).Value };
					break;
				case PublicKeyAlgorithmTag.ElGamalEncrypt:
				case PublicKeyAlgorithmTag.ElGamalGeneral:
					data = new BigInteger[]
					{
						new MPInteger(bcpgIn).Value,
						new MPInteger(bcpgIn).Value
					};
					break;
				default:
					throw new IOException("unknown PGP public key algorithm encountered");
			}
		}

		public PublicKeyEncSessionPacket(
			long					keyId,
			PublicKeyAlgorithmTag	algorithm,
			BigInteger[]			data)
		{
			this.version = 3;
			this.keyId = keyId;
			this.algorithm = algorithm;
			this.data = (BigInteger[]) data.Clone();
		}

		public int Version
		{
			get { return version; }
		}

		public long KeyId
		{
			get { return keyId; }
		}

		public PublicKeyAlgorithmTag Algorithm
		{
			get { return algorithm; }
		}

		public BigInteger[] GetEncSessionKey()
		{
			return (BigInteger[]) data.Clone();
		}

		public override void Encode(
			BcpgOutputStream bcpgOut)
		{
			MemoryStream bOut = new MemoryStream();
			BcpgOutputStream pOut = new BcpgOutputStream(bOut);

			pOut.WriteByte((byte) version);

			pOut.WriteLong(keyId);

			pOut.WriteByte((byte)algorithm);

			for (int i = 0; i != data.Length; i++)
			{
				MPInteger.Encode(pOut, data[i]);
			}

			bcpgOut.WritePacket(PacketTag.PublicKeyEncryptedSession , bOut.ToArray(), true);
		}
	}
}
