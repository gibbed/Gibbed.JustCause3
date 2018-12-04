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

namespace Gibbed.JustCause3.FileFormats
{
    public class AdfFile
    {
        public const uint Signature = 0x41444620; // 'ADF '

        #region Fields
        private Endian _Endian;
        private string _Comment;
        private readonly List<TypeDefinition> _TypeDefinitions;
        private readonly List<InstanceInfo> _InstanceInfos;
        private readonly List<StringHashInfo> _StringHashInfos;
        #endregion

        public AdfFile()
        {
            this._TypeDefinitions = new List<TypeDefinition>();
            this._InstanceInfos = new List<InstanceInfo>();
            this._StringHashInfos = new List<StringHashInfo>();
        }

        #region Properties
        public Endian Endian
        {
            get { return this._Endian; }
            set { this._Endian = value; }
        }

        public string Comment
        {
            get { return this._Comment; }
            set { this._Comment = value; }
        }

        public List<TypeDefinition> TypeDefinitions
        {
            get { return this._TypeDefinitions; }
        }

        public List<InstanceInfo> InstanceInfos
        {
            get { return this._InstanceInfos; }
        }

        public List<StringHashInfo> StringHashInfos
        {
            get { return this._StringHashInfos; }
        }
        #endregion

        public int EstimateHeaderSize()
        {
            return 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 16;
        }

        public void Serialize(Stream output, long headerPosition)
        {
            var endian = this._Endian;
            var names = new StringTable(null);

            long instancePosition = 0;
            long typeDefinitionPosition = 0;
            long stringHashPosition = 0;
            long nameTablePosition = 0;

            if (this._InstanceInfos.Count > 0)
            {
                instancePosition = output.Position;
                foreach (var instanceInfo in this._InstanceInfos)
                {
                    instanceInfo.Write(output, endian, names);
                }
            }

            if (this._TypeDefinitions.Count > 0)
            {
                typeDefinitionPosition = output.Position;
                foreach (var typeDefinition in this._TypeDefinitions)
                {
                    typeDefinition.Write(output, endian, names);
                }
            }

            if (this._StringHashInfos.Count > 0)
            {
                stringHashPosition = output.Position;
                foreach (var stringHashInfo in this._StringHashInfos.OrderBy(shi => shi.ValueHash))
                {
                    stringHashInfo.Write(output, endian);
                }
            }

            if (names.Items.Count > 0)
            {
                nameTablePosition = output.Position;
                var nameBytes = new byte[names.Items.Count][];
                for (int i = 0; i < names.Items.Count; i++)
                {
                    nameBytes[i] = Encoding.ASCII.GetBytes(names.Items[0]);
                    var nameLength = nameBytes[i].Length;
                    if (nameLength > byte.MaxValue)
                    {
                        throw new FormatException();
                    }
                    output.WriteValueU8((byte)nameLength);
                }
                for (int i = 0; i < names.Items.Count; i++)
                {
                    output.WriteBytes(nameBytes[i]);
                }
            }
            var endPosition = output.Position;

            output.Position = headerPosition;
            output.WriteValueU32(Signature, endian);
            output.WriteValueU32(4, endian); // version
            output.WriteValueS32(this._InstanceInfos.Count, endian);
            output.WriteValueU32((uint)(instancePosition - headerPosition), endian);
            output.WriteValueS32(this._TypeDefinitions.Count, endian);
            output.WriteValueU32((uint)(typeDefinitionPosition - headerPosition), endian);
            output.WriteValueS32(this._StringHashInfos.Count, endian);
            output.WriteValueU32((uint)(stringHashPosition - headerPosition), endian);
            output.WriteValueS32(names.Items.Count, endian);
            output.WriteValueU32((uint)(nameTablePosition - headerPosition), endian);
            output.WriteValueU32((uint)(endPosition - headerPosition), endian);
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
            if (version != 4)
            {
                throw new FormatException();
            }

            var instanceCount = input.ReadValueU32(endian);
            var instanceOffset = input.ReadValueU32(endian);
            var typeDefinitionCount = input.ReadValueU32(endian);
            var typeDefinitionOffset = input.ReadValueU32(endian);
            var stringHashCount = input.ReadValueU32(endian);
            var stringHashOffset = input.ReadValueU32(endian);
            var nameTableCount = input.ReadValueU32(endian);
            var nameTableOffset = input.ReadValueU32(endian);
            var totalSize = input.ReadValueU32(endian);
            var unknown2C = input.ReadValueU32(endian);
            var unknown30 = input.ReadValueU32(endian);
            var unknown34 = input.ReadValueU32(endian);
            var unknown38 = input.ReadValueU32(endian);
            var unknown3C = input.ReadValueU32(endian);
            var comment = input.ReadStringZ(Encoding.ASCII);

            if (unknown2C != 0 || unknown30 != 0 || unknown34 != 0 || unknown38 != 0 || unknown3C != 0)
            {
                throw new FormatException();
            }

            if (basePosition + totalSize > input.Length)
            {
                throw new EndOfStreamException();
            }

            var rawNames = new string[nameTableCount];
            if (nameTableCount > 0)
            {
                input.Position = basePosition + nameTableOffset;
                var nameLengths = new byte[nameTableCount];
                for (uint i = 0; i < nameTableCount; i++)
                {
                    nameLengths[i] = input.ReadValueU8();
                }
                for (uint i = 0; i < nameTableCount; i++)
                {
                    rawNames[i] = input.ReadString(nameLengths[i], true, Encoding.ASCII);
                    input.Seek(1, SeekOrigin.Current);
                }
            }
            var names = new StringTable(rawNames);

            var typeDefinitions = new TypeDefinition[typeDefinitionCount];
            if (typeDefinitionCount > 0)
            {
                input.Position = basePosition + typeDefinitionOffset;
                for (uint i = 0; i < typeDefinitionCount; i++)
                {
                    typeDefinitions[i] = TypeDefinition.Read(input, endian, names);
                }
            }

            var instanceInfos = new InstanceInfo[instanceCount];
            if (instanceCount > 0)
            {
                input.Position = basePosition + instanceOffset;
                for (uint i = 0; i < instanceCount; i++)
                {
                    instanceInfos[i] = InstanceInfo.Read(input, endian, names);
                }
            }

            var stringHashInfos = new StringHashInfo[stringHashCount];
            if (stringHashCount > 0)
            {
                input.Position = basePosition + stringHashOffset;
                for (uint i = 0; i < stringHashCount; i++)
                {
                    stringHashInfos[i] = StringHashInfo.Read(input, endian);
                }
            }

            this._Endian = endian;
            this._Comment = comment;
            this._TypeDefinitions.Clear();
            this._TypeDefinitions.AddRange(typeDefinitions);
            this._InstanceInfos.Clear();
            this._InstanceInfos.AddRange(instanceInfos);
            this._StringHashInfos.Clear();
            this._StringHashInfos.AddRange(stringHashInfos);
        }

        public enum TypeDefinitionType : uint
        {
            Primitive = 0,
            Structure = 1,
            Pointer = 2,
            Array = 3,
            InlineArray = 4,
            String = 5,
            BitField = 7,
            Enumeration = 8,
            StringHash = 9,
        }

        public struct TypeDefinition
        {
            public TypeDefinitionType Type;
            public uint Size;
            public uint Alignment;
            public uint NameHash;
            public string Name;
            public uint Flags;
            public uint ElementTypeHash;
            public uint ElementLength;
            public MemberDefinition[] Members;

            internal static TypeDefinition Read(Stream input, Endian endian, StringTable stringTable)
            {
                var instance = new TypeDefinition();
                instance.Type = (TypeDefinitionType)input.ReadValueU32(endian);
                instance.Size = input.ReadValueU32(endian);
                instance.Alignment = input.ReadValueU32(endian);
                instance.NameHash = input.ReadValueU32(endian);
                var nameIndex = input.ReadValueS64(endian);
                instance.Name = stringTable.Get(nameIndex);
                instance.Flags = input.ReadValueU32(endian);
                instance.ElementTypeHash = input.ReadValueU32(endian);
                instance.ElementLength = input.ReadValueU32(endian);

                switch (instance.Type)
                {
                    case TypeDefinitionType.Structure:
                    {
                        var memberCount = input.ReadValueU32(endian);
                        instance.Members = new MemberDefinition[memberCount];
                        for (uint i = 0; i < memberCount; i++)
                        {
                            instance.Members[i] = MemberDefinition.Read(input, endian, stringTable);
                        }
                        break;
                    }

                    case TypeDefinitionType.Array:
                    {
                        var memberCount = input.ReadValueU32(endian);
                        if (memberCount != 0)
                        {
                            throw new FormatException();
                        }
                        break;
                    }

                    case TypeDefinitionType.InlineArray:
                    {
                        var unknown = input.ReadValueU32(endian);
                        if (unknown != 0)
                        {
                            throw new FormatException();
                        }
                        break;
                    }

                    case TypeDefinitionType.Pointer:
                    {
                        var unknown = input.ReadValueU32(endian);
                        if (unknown != 0)
                        {
                            throw new FormatException();
                        }
                        break;
                    }

                    case TypeDefinitionType.StringHash:
                    {
                        var unknown = input.ReadValueU32(endian);
                        if (unknown != 0)
                        {
                            throw new FormatException();
                        }
                        break;
                    }

                    default:
                    {
                        throw new NotSupportedException();
                    }
                }

                return instance;
            }

            internal void Write(Stream output, Endian endian, StringTable stringTable)
            {
                throw new NotImplementedException();
            }

            public override string ToString()
            {
                return string.Format("{0} ({1:X})", this.Name, this.NameHash);
            }
        }

        public struct MemberDefinition
        {
            public string Name;
            public uint TypeHash;
            public uint Size;
            public uint Offset;
            public uint DefaultType;
            public ulong DefaultValue;

            internal static MemberDefinition Read(Stream input, Endian endian, StringTable stringTable)
            {
                var instance = new MemberDefinition();
                var nameIndex = input.ReadValueS64(endian);
                instance.Name = stringTable.Get(nameIndex);
                instance.TypeHash = input.ReadValueU32(endian);
                instance.Size = input.ReadValueU32(endian);
                instance.Offset = input.ReadValueU32(endian);
                instance.DefaultType = input.ReadValueU32(endian);
                instance.DefaultValue = input.ReadValueU64(endian);
                return instance;
            }

            internal void Write(Stream output, Endian endian, StringTable stringTable)
            {
                throw new NotImplementedException();
            }

            public override string ToString()
            {
                return string.Format("{0} ({1:X}) @ {2:X} ({3}, {4})",
                                     this.Name,
                                     this.TypeHash,
                                     this.Offset,
                                     this.DefaultType,
                                     this.DefaultValue);
            }
        }

        public struct InstanceInfo
        {
            public uint NameHash;
            public uint TypeHash;
            public uint Offset;
            public uint Size;
            public string Name;

            internal static InstanceInfo Read(Stream input, Endian endian, StringTable stringTable)
            {
                var instance = new InstanceInfo();
                instance.NameHash = input.ReadValueU32(endian);
                instance.TypeHash = input.ReadValueU32(endian);
                instance.Offset = input.ReadValueU32(endian);
                instance.Size = input.ReadValueU32(endian);
                var nameIndex = input.ReadValueS64(endian);
                instance.Name = stringTable.Get(nameIndex);
                return instance;
            }

            internal static void Write(Stream output, InstanceInfo instance, Endian endian, StringTable stringTable)
            {
                output.WriteValueU32(instance.NameHash, endian);
                output.WriteValueU32(instance.TypeHash, endian);
                output.WriteValueU32(instance.Offset, endian);
                output.WriteValueU32(instance.Size, endian);
                var nameIndex = stringTable.Put(instance.Name);
                output.WriteValueS64(nameIndex, endian);
            }

            internal void Write(Stream output, Endian endian, StringTable stringTable)
            {
                Write(output, this, endian, stringTable);
            }

            public override string ToString()
            {
                return string.Format("{0} ({1:X})", this.Name, this.TypeHash);
            }
        }

        public struct StringHashInfo : IEquatable<StringHashInfo>
        {
            public string Value;
            public uint ValueHash;
            public uint Unknown;

            internal static StringHashInfo Read(Stream input, Endian endian)
            {
                var instance = new StringHashInfo();
                instance.Value = input.ReadStringZ(Encoding.ASCII);
                instance.ValueHash = input.ReadValueU32(endian);
                instance.Unknown = input.ReadValueU32(endian);
                return instance;
            }

            internal static void Write(Stream output, StringHashInfo instance, Endian endian)
            {
                output.WriteStringZ(instance.Value ?? "");
                output.WriteValueU32(instance.ValueHash, endian);
                output.WriteValueU32(instance.Unknown, endian);
            }

            public void Write(Stream output, Endian endian)
            {
                Write(output, this, endian);
            }

            public override string ToString()
            {
                return string.Format("{1:X} = {0} ({2})", this.Value, this.ValueHash, this.Unknown);
            }

            public bool Equals(StringHashInfo other)
            {
                return string.Equals(this.Value, other.Value) == true &&
                       this.Unknown == other.Unknown &&
                       this.ValueHash == other.ValueHash;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj) == true)
                {
                    return false;
                }

                return obj is StringHashInfo && Equals((StringHashInfo)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = (int)this.Unknown;
                    hashCode = (hashCode * 397) ^ (int)this.ValueHash;
                    hashCode = (hashCode * 397) ^ (this.Value != null ? this.Value.GetHashCode() : 0);
                    return hashCode;
                }
            }

            public static bool operator ==(StringHashInfo left, StringHashInfo right)
            {
                return left.Equals(right) == true;
            }

            public static bool operator !=(StringHashInfo left, StringHashInfo right)
            {
                return left.Equals(right) == false;
            }
        }

        internal class StringTable
        {
            private readonly List<string> _Items;

            public StringTable(string[] names)
            {
                this._Items = names == null ? new List<string>() : new List<string>(names);
            }

            public List<string> Items
            {
                get { return this._Items; }
            }

            public string Get(long index)
            {
                if (index < 0 || index >= this._Items.Count || index > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                return this._Items[(int)index];
            }

            public long Put(string text)
            {
                var index = this._Items.IndexOf(text);
                if (index >= 0)
                {
                    return index;
                }
                index = this._Items.Count;
                this._Items.Add(text);
                return index;
            }
        }
    }
}
