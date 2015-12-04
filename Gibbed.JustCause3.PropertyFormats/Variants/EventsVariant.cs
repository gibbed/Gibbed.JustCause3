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
using System.Linq;
using Gibbed.IO;

namespace Gibbed.JustCause3.PropertyFormats.Variants
{
    public class EventsVariant : IVariant, PropertyContainerFile.IRawVariant
    {
        private readonly List<KeyValuePair<uint, uint>> _Values;

        public EventsVariant()
        {
            this._Values = new List<KeyValuePair<uint, uint>>();
        }

        public List<KeyValuePair<uint, uint>> Values
        {
            get { return this._Values; }
        }

        public string Tag
        {
            get { return "vec_events"; }
        }

        public void Parse(string text)
        {
            this._Values.Clear();
            if (string.IsNullOrEmpty(text) == false)
            {
                var parts = text.Split(',');
                if ((parts.Length % 2) != 0)
                {
                    throw new FormatException("vec_events requires pairs of uints delimited by a comma");
                }
                for (int i = 0; i < parts.Length; i += 2)
                {
                    var left = uint.Parse(parts[i + 0], CultureInfo.InvariantCulture);
                    var right = uint.Parse(parts[i + 1], CultureInfo.InvariantCulture);
                    this._Values.Add(new KeyValuePair<uint, uint>(left, right));
                }
            }
        }

        public string Compose()
        {
            return string.Join(", ", this._Values.Select(v => Compose(v)));
        }

        private static string Compose(KeyValuePair<uint, uint> kv)
        {
            return string.Format(
                "{0},{1}",
                kv.Key.ToString(CultureInfo.InvariantCulture),
                kv.Value.ToString(CultureInfo.InvariantCulture));
        }

        #region PropertyContainerFile
        PropertyContainerFile.VariantType PropertyContainerFile.IRawVariant.Type
        {
            get { return PropertyContainerFile.VariantType.Events; }
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
            var values = this._Values;
            output.WriteValueS32(values.Count, endian);
            foreach (var value in values)
            {
                output.WriteValueU32(value.Key, endian);
                output.WriteValueU32(value.Value, endian);
            }
        }

        void PropertyContainerFile.IRawVariant.Deserialize(Stream input, Endian endian)
        {
            int count = input.ReadValueS32(endian);
            var values = new KeyValuePair<uint, uint>[count];
            for (int i = 0; i < count; i++)
            {
                var left = input.ReadValueU32(endian);
                var right = input.ReadValueU32(endian);
                values[i] = new KeyValuePair<uint, uint>(left, right);
            }
            this._Values.Clear();
            this._Values.AddRange(values);
        }
        #endregion
    }
}
