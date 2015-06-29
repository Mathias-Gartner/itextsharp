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

using Org.BouncyCastle.Asn1.Cms;

namespace Org.BouncyCastle.Asn1.Crmf
{
    public class PopoPrivKey
        : Asn1Encodable, IAsn1Choice
    {
        public const int thisMessage = 0;
        public const int subsequentMessage = 1;
        public const int dhMAC = 2;
        public const int agreeMAC = 3;
        public const int encryptedKey = 4;

        private readonly int tagNo;
        private readonly Asn1Encodable obj;

        private PopoPrivKey(Asn1TaggedObject obj)
        {
            this.tagNo = obj.TagNo;

            switch (tagNo)
            {
            case thisMessage:
                this.obj = DerBitString.GetInstance(obj, false);
                break;
            case subsequentMessage:
                this.obj = SubsequentMessage.ValueOf(DerInteger.GetInstance(obj, false).Value.IntValue);
                break;
            case dhMAC:
                this.obj = DerBitString.GetInstance(obj, false);
                break;
            case agreeMAC:
                this.obj = PKMacValue.GetInstance(obj, false);
                break;
            case encryptedKey:
                this.obj = EnvelopedData.GetInstance(obj, false);
                break;
            default:
                throw new ArgumentException("unknown tag in PopoPrivKey", "obj");
            }
        }

        public static PopoPrivKey GetInstance(Asn1TaggedObject tagged, bool isExplicit)
        {
            return new PopoPrivKey(Asn1TaggedObject.GetInstance(tagged.GetObject()));
        }

        public PopoPrivKey(SubsequentMessage msg)
        {
            this.tagNo = subsequentMessage;
            this.obj = msg;
        }

        public virtual int Type
        {
            get { return tagNo; }
        }

        public virtual Asn1Encodable Value
        {
            get { return obj; }
        }

        /**
         * <pre>
         * PopoPrivKey ::= CHOICE {
         *        thisMessage       [0] BIT STRING,         -- Deprecated
         *         -- possession is proven in this message (which contains the private
         *         -- key itself (encrypted for the CA))
         *        subsequentMessage [1] SubsequentMessage,
         *         -- possession will be proven in a subsequent message
         *        dhMAC             [2] BIT STRING,         -- Deprecated
         *        agreeMAC          [3] PKMACValue,
         *        encryptedKey      [4] EnvelopedData }
         * </pre>
         */
        public override Asn1Object ToAsn1Object()
        {
            return new DerTaggedObject(false, tagNo, obj);
        }
    }
}
