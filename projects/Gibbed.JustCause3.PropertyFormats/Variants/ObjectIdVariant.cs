/* Copyright (c) 2015 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Gibbed.IO;

namespace Gibbed.JustCause3.PropertyFormats.Variants
{
    public class ObjectIdVariant : IVariant, PropertyContainerFile.IRawVariant
    {
        private KeyValuePair<uint, uint> _Value;

        public KeyValuePair<uint, uint> Value
        {
            get { return this._Value; }
            set { this._Value = value; }
        }

        public string Tag
        {
            get { return "objectid"; }
        }

        public void Parse(string text)
        {
            var parts = text.Split(',');
            if (parts.Length != 2)
            {
                throw new FormatException("objectid requires a pair of uints delimited by a comma");
            }

            var left = uint.Parse(parts[0], CultureInfo.InvariantCulture);
            var right = uint.Parse(parts[1], CultureInfo.InvariantCulture);
            this._Value = new KeyValuePair<uint, uint>(left, right);
        }

        public string Compose()
        {
            return String.Format(
                "{0},{1}",
                this._Value.Key.ToString(CultureInfo.InvariantCulture),
                this._Value.Value.ToString(CultureInfo.InvariantCulture));
        }

        #region PropertyContainerFile
        PropertyContainerFile.VariantType PropertyContainerFile.IRawVariant.Type
        {
            get { return PropertyContainerFile.VariantType.ObjectId; }
        }

        bool PropertyContainerFile.IRawVariant.IsPrimitive
        {
            get { return false; }
        }

        uint PropertyContainerFile.IRawVariant.Alignment
        {
            get { return 4; }
        }

        void PropertyContainerFile.IRawVariant.Serialize(Stream output, Endian endian)
        {
            output.WriteValueU32(this._Value.Key, endian);
            output.WriteValueU32(this._Value.Value, endian);
        }

        void PropertyContainerFile.IRawVariant.Deserialize(Stream input, Endian endian)
        {
            var left = input.ReadValueU32(endian);
            var right = input.ReadValueU32(endian);
            this._Value = new KeyValuePair<uint, uint>(left, right);
        }
        #endregion
    }
}
