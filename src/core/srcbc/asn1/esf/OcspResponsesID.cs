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

namespace Org.BouncyCastle.Asn1.Esf
{
	/// <remarks>
	/// RFC 3126: 4.2.2 Complete Revocation Refs Attribute Definition
	/// <code>
	/// OcspResponsesID ::= SEQUENCE {
	///		ocspIdentifier	OcspIdentifier,
	///		ocspRepHash		OtherHash OPTIONAL
	/// }
	/// </code>
	/// </remarks>
	public class OcspResponsesID
		: Asn1Encodable
	{
		private readonly OcspIdentifier	ocspIdentifier;
		private readonly OtherHash		ocspRepHash;

		public static OcspResponsesID GetInstance(
			object obj)
		{
			if (obj == null || obj is OcspResponsesID)
				return (OcspResponsesID) obj;

			if (obj is Asn1Sequence)
				return new OcspResponsesID((Asn1Sequence) obj);

			throw new ArgumentException(
				"Unknown object in 'OcspResponsesID' factory: "
					+ obj.GetType().Name,
				"obj");
		}

		private OcspResponsesID(
			Asn1Sequence seq)
		{
			if (seq == null)
				throw new ArgumentNullException("seq");
			if (seq.Count < 1 || seq.Count > 2)
				throw new ArgumentException("Bad sequence size: " + seq.Count, "seq");

			this.ocspIdentifier = OcspIdentifier.GetInstance(seq[0].ToAsn1Object());

			if (seq.Count > 1)
			{
				this.ocspRepHash = OtherHash.GetInstance(seq[1].ToAsn1Object());
			}
		}

		public OcspResponsesID(
			OcspIdentifier ocspIdentifier)
			: this(ocspIdentifier, null)
		{
		}

		public OcspResponsesID(
			OcspIdentifier	ocspIdentifier,
			OtherHash		ocspRepHash)
		{
			if (ocspIdentifier == null)
				throw new ArgumentNullException("ocspIdentifier");

			this.ocspIdentifier = ocspIdentifier;
			this.ocspRepHash = ocspRepHash;
		}

		public OcspIdentifier OcspIdentifier
		{
			get { return ocspIdentifier; }
		}

		public OtherHash OcspRepHash
		{
			get { return ocspRepHash; }
		}

		public override Asn1Object ToAsn1Object()
		{
			Asn1EncodableVector v = new Asn1EncodableVector(
				ocspIdentifier.ToAsn1Object());

			if (ocspRepHash != null)
			{
				v.Add(ocspRepHash.ToAsn1Object());
			}

			return new DerSequence(v);
		}

	}
}
