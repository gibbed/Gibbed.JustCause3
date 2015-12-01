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
using System.IO;
using System.Linq;
using System.Text;
using Gibbed.IO;
using Gibbed.JustCause3.FileFormats;

namespace Gibbed.JustCause3.PropertyFormats
{
    public class RawPropertyContainerFile : IPropertyFile
    {
        private Endian _Endian;
        private readonly List<Node> _Nodes;

        public RawPropertyContainerFile()
        {
            this._Nodes = new List<Node>();
        }

        public Endian Endian
        {
            get { return this._Endian; }
            set { this._Endian = value; }
        }

        public List<Node> Nodes
        {
            get { return this._Nodes; }
        }

        // ReSharper disable InconsistentNaming
        internal enum VariantType : byte
        {
            Unassigned = 0,
            Integer = 1,
            Float = 2,
            String = 3,
            Vector2 = 4,
            Vector3 = 5,
            Vector4 = 6,

            [Obsolete]
            DoNotUse1 = 7, // Matrix3x3

            Matrix4x3 = 8,
            Integers = 9,
            Floats = 10,
            Bytes = 11,

            [Obsolete]
            DoNutUse2 = 12,

            ObjectId = 13,
            Events = 14,
        }

        // ReSharper restore InconsistentNaming

        internal interface IRawVariant
        {
            VariantType Type { get; }

            void Serialize(Stream output, Endian endian);
            void Deserialize(Stream input, Endian endian);
        }

        #region SectionType
        private enum SectionType : byte
        {
            // ReSharper disable UnusedMember.Local
            Invalid = 0,
            // ReSharper restore UnusedMember.Local
            Node = 1,
            Property = 2,
            Tag = 3,
            NodeByHash = 4,
            PropertyByHash = 5,
        }
        #endregion

        private void SerializeVariant(Stream output, IVariant variant)
        {
            var endian = this._Endian;

            var rawVariant = variant as IRawVariant;
            if (rawVariant == null)
            {
                throw new ArgumentException("variant is not a raw variant", "variant");
            }

            output.WriteValueU8((byte)rawVariant.Type);
            rawVariant.Serialize(output, endian);
        }

        private void SerializeNode(Stream output, Node node)
        {
            var endian = this._Endian;

            byte count = 0;

            var childrenByName = node.Children
                                     .Where(kv => node.KnownNames.ContainsKey(kv.Key) == true)
                                     .ToArray();
            var propertiesByName = node.Properties
                                       .Where(kv => node.KnownNames.ContainsKey(kv.Key) == true)
                                       .ToArray();
            var childrenByNameHash = node.Children
                                         .Where(kv => node.KnownNames.ContainsKey(kv.Key) == false)
                                         .ToArray();
            var propertiesByNameHash = node.Properties
                                           .Where(kv => node.KnownNames.ContainsKey(kv.Key) == false)
                                           .ToArray();

            if (childrenByName.Length > 0)
            {
                count++;
            }

            if (propertiesByName.Length > 0)
            {
                count++;
            }

            if (childrenByNameHash.Length > 0)
            {
                count++;
            }

            if (propertiesByNameHash.Length > 0)
            {
                count++;
            }

            if (node.Tag != null)
            {
                count++;
            }

            output.WriteValueU8(count);

            if (childrenByName.Length > 0)
            {
                if (childrenByName.Length > 0xFFFF)
                {
                    throw new InvalidOperationException();
                }

                output.WriteValueU16((ushort)SectionType.Node, endian);
                output.WriteValueU16((ushort)childrenByName.Length, endian);
                foreach (var kv in childrenByName)
                {
                    var name = node.KnownNames[kv.Key];
                    output.WriteValueS32(name.Length, endian);
                    output.WriteString(name, Encoding.ASCII);
                    this.SerializeNode(output, kv.Value);
                }
            }

            if (propertiesByName.Length > 0)
            {
                if (propertiesByName.Length > 0xFFFF)
                {
                    throw new InvalidOperationException();
                }

                output.WriteValueU16((ushort)SectionType.Property, endian);
                output.WriteValueU16((ushort)propertiesByName.Length, endian);
                foreach (var kv in propertiesByName)
                {
                    var name = node.KnownNames[kv.Key];
                    output.WriteValueS32(name.Length, endian);
                    output.WriteString(name, Encoding.ASCII);
                    this.SerializeVariant(output, kv.Value);
                }
            }

            if (node.Tag != null)
            {
                var bytes = Encoding.ASCII.GetBytes(node.Tag);

                if (bytes.Length > 0xFFFF)
                {
                    throw new InvalidOperationException();
                }

                output.WriteValueU16(3, endian);
                output.WriteValueU16((ushort)bytes.Length, endian);
                output.WriteBytes(bytes);
            }

            if (childrenByNameHash.Length > 0)
            {
                if (childrenByNameHash.Length > 0xFFFF)
                {
                    throw new InvalidOperationException();
                }

                output.WriteValueU16((ushort)SectionType.NodeByHash, endian);
                output.WriteValueU16((ushort)childrenByNameHash.Length, endian);
                foreach (var kv in childrenByNameHash)
                {
                    output.WriteValueU32(kv.Key, endian);
                    this.SerializeNode(output, kv.Value);
                }
            }

            if (propertiesByNameHash.Length > 0)
            {
                if (propertiesByNameHash.Length > 0xFFFF)
                {
                    throw new InvalidOperationException();
                }

                output.WriteValueU16((ushort)SectionType.PropertyByHash, endian);
                output.WriteValueU16((ushort)propertiesByNameHash.Length, endian);
                foreach (var kv in propertiesByNameHash)
                {
                    output.WriteValueU32(kv.Key, endian);
                    this.SerializeVariant(output, kv.Value);
                }
            }
        }

        public void Serialize(Stream output)
        {
            foreach (var node in this.Nodes)
            {
                this.SerializeNode(output, node);
            }
        }

        private IVariant DeserializeVariant(Stream input)
        {
            var endian = this._Endian;
            var type = (VariantType)input.ReadValueU8();

            var rawVariant = VariantFactory.GetVariant(type);
            rawVariant.Deserialize(input, endian);
            return (IVariant)rawVariant;
        }

        private Node DeserializeNode(Stream input)
        {
            var endian = this._Endian;
            var node = new Node();

            var sectionsHandled = new List<SectionType>();
            var sectionCount = input.ReadValueU8();

            for (byte i = 0; i < sectionCount; i++)
            {
                var sectionType = (SectionType)input.ReadValueU16(endian);
                var elementCount = input.ReadValueU16(endian);

                if (sectionsHandled.Contains(sectionType) == true)
                {
                    throw new FormatException();
                }
                sectionsHandled.Add(sectionType);

                switch (sectionType)
                {
                    case SectionType.Node:
                    {
                        for (ushort j = 0; j < elementCount; j++)
                        {
                            var length = input.ReadValueU32(endian);
                            if (length >= 0x7FFF)
                            {
                                throw new FormatException();
                            }

                            var name = input.ReadString(length, true, Encoding.ASCII);
                            var id = name.HashJenkins();

                            if (node.KnownNames.ContainsKey(id) == false)
                            {
                                node.KnownNames.Add(id, name);
                            }
                            else if (node.KnownNames[id] != name)
                            {
                                throw new FormatException();
                            }

                            node.Children.Add(id, this.DeserializeNode(input));
                        }

                        break;
                    }

                    case SectionType.Property:
                    {
                        for (ushort j = 0; j < elementCount; j++)
                        {
                            var length = input.ReadValueU32(endian);
                            if (length >= 0x7FFF)
                            {
                                throw new FormatException();
                            }

                            var name = input.ReadString(length, true, Encoding.ASCII);
                            var id = name.HashJenkins();

                            if (node.KnownNames.ContainsKey(id) == false)
                            {
                                node.KnownNames.Add(id, name);
                            }
                            else if (node.KnownNames[id] != name)
                            {
                                throw new FormatException();
                            }

                            node.Properties.Add(id, this.DeserializeVariant(input));
                        }

                        break;
                    }

                    case SectionType.Tag:
                    {
                        node.Tag = input.ReadString(elementCount, Encoding.ASCII);
                        break;
                    }

                    case SectionType.NodeByHash:
                    {
                        for (ushort j = 0; j < elementCount; j++)
                        {
                            var id = input.ReadValueU32(endian);
                            node.Children.Add(id, this.DeserializeNode(input));
                        }

                        break;
                    }

                    case SectionType.PropertyByHash:
                    {
                        for (ushort j = 0; j < elementCount; j++)
                        {
                            var id = input.ReadValueU32(endian);
                            node.Properties.Add(id, this.DeserializeVariant(input));
                        }

                        break;
                    }

                    default:
                    {
                        throw new FormatException("unknown object section type " +
                                                  sectionType.ToString());
                    }
                }
            }

            return node;
        }

        public void Deserialize(Stream input)
        {
            input.Seek(0, SeekOrigin.Begin);

            this.Nodes.Clear();
            while (input.Position < input.Length)
            {
                this.Nodes.Add(this.DeserializeNode(input));
            }

            if (input.Position != input.Length)
            {
                throw new FormatException();
            }
        }

        public void Deserialize(Stream input, int length)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (length < 0 ||
                input.Position + length > input.Length)
            {
                throw new ArgumentOutOfRangeException("length");
            }

            var end = input.Position + length;

            this.Nodes.Clear();
            while (input.Position < end)
            {
                this.Nodes.Add(this.DeserializeNode(input));
            }

            if (input.Position != end)
            {
                throw new FormatException();
            }
        }
    }
}
