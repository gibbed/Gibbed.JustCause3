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
using System.Text;
using System.Xml;
using Gibbed.IO;
using Gibbed.JustCause3.FileFormats;
using MemberDefinition = Gibbed.JustCause3.FileFormats.AdfFile.MemberDefinition;
using TypeDefinition = Gibbed.JustCause3.FileFormats.AdfFile.TypeDefinition;
using TypeDefinitionType = Gibbed.JustCause3.FileFormats.AdfFile.TypeDefinitionType;

namespace Gibbed.JustCause3.ConvertAdf
{
    internal static class Exporter
    {
        public static void Export(AdfFile adf, RuntimeTypeLibrary runtime, Stream input, XmlWriter writer)
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("adf");

            if (adf.InstanceInfos.Count > 0)
            {
                writer.WriteStartElement("instances");

                foreach (var instanceInfo in adf.InstanceInfos)
                {
                    writer.WriteStartElement("instance");
                    writer.WriteAttributeString("root", instanceInfo.Name);

                    var typeDefinition = runtime.GetTypeDefinition(instanceInfo.TypeHash);
                    input.Position = instanceInfo.Offset;
                    using (var data = input.ReadToMemoryStream((int)instanceInfo.Size))
                    {
                        var exporter = new InstanceExporter(adf, runtime);
                        exporter.Write(typeDefinition, instanceInfo.Name, data, writer);
                    }

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        private class InstanceExporter
        {
            private readonly AdfFile _Adf;
            private readonly RuntimeTypeLibrary _Runtime;
            private long _Counter;
            private readonly Queue<WorkItem> _WorkQueue;

            public InstanceExporter(AdfFile adf, RuntimeTypeLibrary runtime)
            {
                if (adf == null)
                {
                    throw new ArgumentNullException("adf");
                }

                if (runtime == null)
                {
                    throw new ArgumentNullException("runtime");
                }

                this._Adf = adf;
                this._Runtime = runtime;
                this._WorkQueue = new Queue<WorkItem>();
            }

            public void Write(TypeDefinition rootTypeDefinition, string name, MemoryStream data, XmlWriter writer)
            {
                this._Counter = 0;
                this._WorkQueue.Clear();
                this._WorkQueue.Enqueue(new WorkItem(this._Counter++, name, rootTypeDefinition, 0));
                while (this._WorkQueue.Count > 0)
                {
                    var workItem = this._WorkQueue.Dequeue();

                    switch (workItem.TypeDefinition.Type)
                    {
                        case TypeDefinitionType.Structure:
                        {
                            data.Position = workItem.Offset;
                            WriteStructure(writer, workItem.TypeDefinition, workItem.Id, workItem.Name, data);
                            break;
                        }

                        case TypeDefinitionType.Array:
                        {
                            data.Position = workItem.Offset;
                            WriteArray(writer, workItem.TypeDefinition, workItem.Id, data);
                            break;
                        }

                        default:
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
            }

            private struct WorkItem
            {
                public long Id;
                public string Name;
                public TypeDefinition TypeDefinition;
                public long Offset;

                public WorkItem(long id, string name, TypeDefinition typeDefinition, long offset)
                {
                    this.Id = id;
                    this.Name = name;
                    this.TypeDefinition = typeDefinition;
                    this.Offset = offset;
                }
            }

            private void WriteStructure(XmlWriter writer,
                                        TypeDefinition typeDefinition,
                                        long id,
                                        string name,
                                        MemoryStream data)
            {
                var basePosition = data.Position;

                writer.WriteStartElement("struct");
                writer.WriteAttributeString("type", typeDefinition.Name);

                if (name != null)
                {
                    writer.WriteAttributeString("name", name);
                }

                if (id >= 0)
                {
                    writer.WriteAttributeString("id", "#" + id);
                }

                foreach (var memberDefinition in typeDefinition.Members)
                {
                    data.Position = basePosition + memberDefinition.Offset;
                    this.WriteMember(writer, data, memberDefinition);
                }

                writer.WriteEndElement();
            }

            private void WriteMember(XmlWriter writer, MemoryStream data, MemberDefinition memberDefinition)
            {
                writer.WriteStartElement("member");
                writer.WriteAttributeString("name", memberDefinition.Name);

                switch (memberDefinition.TypeHash)
                {
                    case TypeHashes.Primitive.Int8:
                    {
                        var value = data.ReadValueS8();
                        writer.WriteValue(value.ToString(CultureInfo.InvariantCulture));
                        break;
                    }

                    case TypeHashes.Primitive.UInt8:
                    {
                        var value = data.ReadValueU8();
                        writer.WriteValue(value.ToString(CultureInfo.InvariantCulture));
                        break;
                    }

                    case TypeHashes.Primitive.UInt16:
                    {
                        var value = data.ReadValueU16(this._Adf.Endian);
                        writer.WriteValue(value.ToString(CultureInfo.InvariantCulture));
                        break;
                    }

                    case TypeHashes.Primitive.Int32:
                    {
                        var value = data.ReadValueS32(this._Adf.Endian);
                        writer.WriteValue(value.ToString(CultureInfo.InvariantCulture));
                        break;
                    }

                    case TypeHashes.Primitive.UInt32:
                    {
                        var value = data.ReadValueU32(this._Adf.Endian);
                        writer.WriteValue(value.ToString(CultureInfo.InvariantCulture));
                        break;
                    }

                    case TypeHashes.Primitive.Int64:
                    {
                        var value = data.ReadValueS64(this._Adf.Endian);
                        writer.WriteValue(value.ToString(CultureInfo.InvariantCulture));
                        break;
                    }

                    case TypeHashes.Primitive.UInt64:
                    {
                        var value = data.ReadValueU64(this._Adf.Endian);
                        writer.WriteValue(value.ToString(CultureInfo.InvariantCulture));
                        break;
                    }

                    case TypeHashes.Primitive.Float:
                    {
                        var value = data.ReadValueF32(this._Adf.Endian);
                        writer.WriteValue(value.ToString(CultureInfo.InvariantCulture));
                        break;
                    }

                    case TypeHashes.String:
                    {
                        var offset = data.ReadValueS64(this._Adf.Endian);
                        data.Position = offset;
                        var value = data.ReadStringZ(Encoding.UTF8);
                        writer.WriteValue(value);
                        break;
                    }

                    default:
                    {
                        var typeDefinition = this._Runtime.GetTypeDefinition(memberDefinition.TypeHash);
                        switch (typeDefinition.Type)
                        {
                            case TypeDefinitionType.Structure:
                            {
                                this.WriteStructure(writer, typeDefinition, -1, null, data);
                                break;
                            }

                            case TypeDefinitionType.Array:
                            {
                                var id = this._Counter++;
                                this._WorkQueue.Enqueue(new WorkItem(id, null, typeDefinition, data.Position));
                                writer.WriteValue("#" + id.ToString(CultureInfo.InvariantCulture));
                                break;
                            }

                            case TypeDefinitionType.InlineArray:
                            {
                                this.WriteArrayItems(writer,
                                                     typeDefinition.ElementTypeHash,
                                                     -1,
                                                     data,
                                                     typeDefinition.ElementLength);
                                break;
                            }

                            case TypeDefinitionType.StringHash:
                            {
                                var value = data.ReadValueU32(this._Adf.Endian);
                                var stringHashInfo = this._Adf
                                                         .StringHashInfos
                                                         .FirstOrDefault(shi => shi.ValueHash == value);
                                if (stringHashInfo != default(AdfFile.StringHashInfo))
                                {
                                    writer.WriteValue(stringHashInfo.Value);
                                }
                                else
                                {
                                    writer.WriteValue(value);
                                }

                                break;
                            }

                            default:
                            {
                                throw new NotSupportedException();
                            }
                        }

                        break;
                    }
                }

                writer.WriteEndElement();
            }

            private void WriteArray(XmlWriter writer, TypeDefinition typeDefinition, long id, MemoryStream data)
            {
                var endian = this._Adf.Endian;
                var offset = data.ReadValueS64(endian);
                var count = data.ReadValueS64(endian);
                data.Position = offset;
                this.WriteArrayItems(writer,
                                     typeDefinition.ElementTypeHash,
                                     id,
                                     data,
                                     count);
            }

            private void WriteArrayItems(XmlWriter writer, uint elementTypeHash, long id, MemoryStream data, long count)
            {
                writer.WriteStartElement("array");

                if (id >= 0)
                {
                    writer.WriteAttributeString("id", "#" + id);
                }

                switch (elementTypeHash)
                {
                    case TypeHashes.Primitive.Int8:
                    {
                        var sb = new StringBuilder();
                        for (long i = 0; i < count; i++)
                        {
                            var value = data.ReadValueS8();
                            sb.Append(value.ToString(CultureInfo.InvariantCulture));
                            sb.Append(" ");
                        }
                        writer.WriteValue(sb.ToString().Trim());
                        break;
                    }

                    case TypeHashes.Primitive.UInt8:
                    {
                        var sb = new StringBuilder();
                        for (long i = 0; i < count; i++)
                        {
                            var value = data.ReadValueU8();
                            sb.Append(value.ToString(CultureInfo.InvariantCulture));
                            sb.Append(" ");
                        }
                        writer.WriteValue(sb.ToString().Trim());
                        break;
                    }

                    case TypeHashes.Primitive.UInt16:
                    {
                        var sb = new StringBuilder();
                        for (long i = 0; i < count; i++)
                        {
                            var value = data.ReadValueU16(this._Adf.Endian);
                            sb.Append(value.ToString(CultureInfo.InvariantCulture));
                            sb.Append(" ");
                        }
                        writer.WriteValue(sb.ToString().Trim());
                        break;
                    }

                    case TypeHashes.Primitive.Int32:
                    {
                        var sb = new StringBuilder();
                        for (long i = 0; i < count; i++)
                        {
                            var value = data.ReadValueS32(this._Adf.Endian);
                            sb.Append(value.ToString(CultureInfo.InvariantCulture));
                            sb.Append(" ");
                        }
                        writer.WriteValue(sb.ToString().Trim());
                        break;
                    }

                    case TypeHashes.Primitive.UInt32:
                    {
                        var sb = new StringBuilder();
                        for (long i = 0; i < count; i++)
                        {
                            var value = data.ReadValueU32(this._Adf.Endian);
                            sb.Append(value.ToString(CultureInfo.InvariantCulture));
                            sb.Append(" ");
                        }
                        writer.WriteValue(sb.ToString().Trim());
                        break;
                    }

                    case TypeHashes.Primitive.Float:
                    {
                        var sb = new StringBuilder();
                        for (long i = 0; i < count; i++)
                        {
                            var value = data.ReadValueF32(this._Adf.Endian);
                            sb.Append(value.ToString(CultureInfo.InvariantCulture));
                            sb.Append(" ");
                        }
                        writer.WriteValue(sb.ToString().Trim());
                        break;
                    }

                    default:
                    {
                        var elementTypeDefinition = this._Runtime.GetTypeDefinition(elementTypeHash);
                        switch (elementTypeDefinition.Type)
                        {
                            case TypeDefinitionType.Structure:
                            {
                                var basePosition = data.Position;
                                for (long i = 0; i < count; i++)
                                {
                                    data.Position = basePosition + (i * elementTypeDefinition.Size);
                                    WriteStructure(writer, elementTypeDefinition, -1, null, data);
                                }
                                break;
                            }

                            case TypeDefinitionType.StringHash:
                            {
                                var sb = new StringBuilder();
                                var basePosition = data.Position;
                                for (long i = 0; i < count; i++)
                                {
                                    data.Position = basePosition + (i * elementTypeDefinition.Size);
                                    var value = data.ReadValueU32(this._Adf.Endian);
                                    var stringHashInfo = this._Adf
                                                             .StringHashInfos
                                                             .FirstOrDefault(shi => shi.ValueHash == value);
                                    if (stringHashInfo != default(AdfFile.StringHashInfo))
                                    {
                                        sb.Append(stringHashInfo.Value);
                                    }
                                    else
                                    {
                                        sb.Append(value.ToString(CultureInfo.InvariantCulture));
                                    }

                                    sb.Append(" ");
                                }
                                writer.WriteValue(sb.ToString().Trim());
                                break;
                            }

                            default:
                            {
                                throw new NotSupportedException();
                            }
                        }
                        break;
                    }
                }

                writer.WriteEndElement();
            }
        }
    }
}
