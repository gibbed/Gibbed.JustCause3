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
using Gibbed.IO;

namespace Gibbed.JustCause3.PropertyFormats
{
    public class PropertyContainerFile : IPropertyFile
    {
        public const uint Signature = 0x43505452; // 'RTPC'

        private Endian _Endian;
        private readonly List<Node> _Nodes;

        public PropertyContainerFile()
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

        public static bool CheckSignature(Stream input)
        {
            var magic = input.ReadValueU32(Endian.Little);
            return magic == Signature || magic.Swap() == Signature;
        }

        public void Serialize(Stream output)
        {
            throw new NotImplementedException();
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
            bool IsSimple { get; }

            void Serialize(Stream output, Endian endian);
            void Deserialize(Stream input, Endian endian);
        }

        private struct RawNode
        {
            public readonly uint NameHash;
            public readonly uint DataOffset;
            public readonly ushort PropertyCount;
            public readonly ushort InstanceCount;

            public RawNode(uint nameHash, uint dataOffset, ushort propertyCount, ushort instanceCount)
            {
                this.NameHash = nameHash;
                this.DataOffset = dataOffset;
                this.PropertyCount = propertyCount;
                this.InstanceCount = instanceCount;
            }

            public static RawNode Read(Stream input, Endian endian)
            {
                var nameHash = input.ReadValueU32(endian);
                var dataOffset = input.ReadValueU32(endian);
                var propertyCount = input.ReadValueU16(endian);
                var instanceCount = input.ReadValueU16(endian);
                return new RawNode(nameHash, dataOffset, propertyCount, instanceCount);
            }
        }

        private struct RawProperty
        {
            public readonly uint NameHash;
            public readonly byte[] Data;
            public readonly VariantType Type;

            public RawProperty(uint nameHash, byte[] data, VariantType type)
            {
                this.NameHash = nameHash;
                this.Data = data;
                this.Type = type;
            }

            public static RawProperty Read(Stream input, Endian endian)
            {
                var nameHash = input.ReadValueU32(endian);
                var data = input.ReadBytes(4);
                var type = (VariantType)input.ReadValueU8();
                return new RawProperty(nameHash, data, type);
            }

            public override string ToString()
            {
                return string.Format("{0:X} {1}", this.NameHash, this.Type);
            }
        }

        public void Deserialize(Stream input)
        {
            var basePosition = input.Position;

            var magic = input.ReadValueU32(Endian.Little);
            if (magic != Signature && magic.Swap() != Signature)
            {
                throw new FormatException();
            }
            var endian = magic == Signature ? Endian.Little : Endian.Big;

            var version = input.ReadValueU32(endian);
            if (version != 1)
            {
                throw new FormatException();
            }

            var rootNode = new Node();
            var rawRootNode = RawNode.Read(input, endian);

            var instanceQueue = new Queue<Tuple<Node, RawNode>>();
            instanceQueue.Enqueue(new Tuple<Node, RawNode>(rootNode, rawRootNode));

            var propertyQueue = new Queue<Tuple<Node, RawProperty[]>>();

            while (instanceQueue.Count > 0)
            {
                var item = instanceQueue.Dequeue();
                var node = item.Item1;
                var rawNode = item.Item2;

                input.Position = basePosition + rawNode.DataOffset;
                var rawProperties = new RawProperty[rawNode.PropertyCount];
                for (int i = 0; i < rawNode.PropertyCount; i++)
                {
                    rawProperties[i] = RawProperty.Read(input, endian);
                }

                input.Position = basePosition + rawNode.DataOffset + (9 * rawNode.PropertyCount).Align(4);
                for (int i = 0; i < rawNode.InstanceCount; i++)
                {
                    var rawChildNode = RawNode.Read(input, endian);
                    var childNode = new Node();
                    node.Children.Add(rawChildNode.NameHash, childNode);
                    instanceQueue.Enqueue(new Tuple<Node, RawNode>(childNode, rawChildNode));
                }

                propertyQueue.Enqueue(new Tuple<Node, RawProperty[]>(node, rawProperties));
            }

            while (propertyQueue.Count > 0)
            {
                var item = propertyQueue.Dequeue();
                var node = item.Item1;
                var rawProperties = item.Item2;

                foreach (var rawProperty in rawProperties)
                {
                    var variant = VariantFactory.GetVariant(rawProperty.Type);

                    if (variant.IsSimple == false)
                    {
                        using (var temp = new MemoryStream(rawProperty.Data, false))
                        {
                            variant.Deserialize(temp, endian);

                            if (temp.Position != temp.Length)
                            {
                                throw new InvalidOperationException();
                            }
                        }
                    }
                    else
                    {
                        if (rawProperty.Data.Length != 4)
                        {
                            throw new InvalidOperationException();
                        }

                        uint offset;
                        using (var temp = new MemoryStream(rawProperty.Data, false))
                        {
                            offset = temp.ReadValueU32(endian);
                        }

                        input.Position = basePosition + offset;
                        variant.Deserialize(input, endian);
                    }

                    node.Properties.Add(rawProperty.NameHash, (IVariant)variant);
                }
            }

            var fauxRootNode = new Node();
            fauxRootNode.Children.Add(rawRootNode.NameHash, rootNode);
            this._Nodes.Add(fauxRootNode);
        }
    }
}
