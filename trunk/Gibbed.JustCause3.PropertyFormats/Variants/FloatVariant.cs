﻿/* Copyright (c) 2015 Rick (rick 'at' gibbed 'dot' us)
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

using System.Globalization;
using System.IO;
using Gibbed.IO;

namespace Gibbed.JustCause3.PropertyFormats.Variants
{
    public class FloatVariant : IVariant, PropertyContainerFile.IRawVariant
    {
        private float _Value;

        public float Value
        {
            get { return this._Value; }
            set { this._Value = value; }
        }

        public string Tag
        {
            get { return "float"; }
        }

        public void Parse(string text)
        {
            this._Value = float.Parse(text, CultureInfo.InvariantCulture);
        }

        public string Compose()
        {
            return this._Value.ToString(CultureInfo.InvariantCulture);
        }

        #region PropertyContainerFile
        PropertyContainerFile.VariantType PropertyContainerFile.IRawVariant.Type
        {
            get { return PropertyContainerFile.VariantType.Float; }
        }

        bool PropertyContainerFile.IRawVariant.IsPrimitive
        {
            get { return true; }
        }

        uint PropertyContainerFile.IRawVariant.Alignment
        {
            get { return 0; }
        }

        void PropertyContainerFile.IRawVariant.Serialize(Stream output, Endian endian)
        {
            output.WriteValueF32(this._Value, endian);
        }

        void PropertyContainerFile.IRawVariant.Deserialize(Stream input, Endian endian)
        {
            this._Value = input.ReadValueF32(endian);
        }
        #endregion
    }
}
