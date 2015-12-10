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
using Gibbed.JustCause3.FileFormats;
using NDesk.Options;
using Newtonsoft.Json;

namespace Gibbed.JustCause3.ConvertItem
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        private static void SetOption<T>(string s, ref T variable, T value)
        {
            if (s == null)
            {
                return;
            }

            variable = value;
        }

        internal enum Mode
        {
            Unknown,
            Export,
            Import,
        }

        private static void Main(string[] args)
        {
            var mode = Mode.Unknown;
            bool showHelp = false;

            var options = new OptionSet
            {
                // ReSharper disable AccessToModifiedClosure
                { "e|export", "convert from binary to JSON", v => SetOption(v, ref mode, Mode.Export) },
                { "i|import", "convert from JSON to binary", v => SetOption(v, ref mode, Mode.Import) },
                // ReSharper restore AccessToModifiedClosure
                { "h|help", "show this message and exit", v => showHelp = v != null },
            };

            List<string> extras;

            try
            {
                extras = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", GetExecutableName());
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", GetExecutableName());
                return;
            }

            if (mode == Mode.Unknown && extras.Count >= 1)
            {
                var extension = Path.GetExtension(extras[0]);
                if (extension != null && extension.ToLowerInvariant() == ".json")
                {
                    mode = Mode.Import;
                }
                else
                {
                    mode = Mode.Export;
                }
            }

            if (extras.Count < 1 || extras.Count > 2 ||
                showHelp == true ||
                mode == Mode.Unknown)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ [-e] input_item_onlinec [output_json]", GetExecutableName());
                Console.WriteLine("       {0} [OPTIONS]+ [-i] input_json [output_item_onlinec]", GetExecutableName());
                Console.WriteLine("Convert an item file between binary and JSON format.");
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            if (mode == Mode.Export)
            {
                string inputPath = extras[0];
                string outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputPath, ".json");

                var adf = new AdfFile();
                var itemRoot = new ItemRoot();

                using (var input = File.OpenRead(inputPath))
                {
                    adf.Deserialize(input);

                    var itemRootInfo = adf.InstanceInfos.FirstOrDefault(i => i.Name == "Profile");
                    if (itemRootInfo.TypeHash != ItemRoot.TypeHash)
                    {
                        throw new FormatException();
                    }

                    input.Position = itemRootInfo.Offset;
                    itemRoot.Deserialize(input, adf.Endian);
                }

                using (var output = File.Create(outputPath))
                using (var textWriter = new StreamWriter(output))
                using (var writer = new JsonTextWriter(textWriter))
                {
                    writer.Indentation = 2;
                    writer.IndentChar = ' ';
                    writer.Formatting = Formatting.Indented;

                    var jsonItems = itemRoot.Items.Select(
                        i =>
                        {
                            var jsonItem = new ItemRootJson.Item()
                            {
                                Name = ResolveHash(adf, i.Name),
                                BaseName = ResolveHash(adf, i.BaseName),
                                Type = i.Type,
                                Purchasable = i.Purchasable,
                                Consumable = i.Consumable,
                                Rewardable = i.Rewardable,
                                Collectable = i.Collectable,
                                Region = ResolveHash(adf, i.Region),
                                RebelDropTimer = i.RebelDropTimer,
                                MaxInventory = i.MaxInventory,
                                UIType = (UIItemType)i.UIType,
                                UIFlag = (UIItemSubtype)i.UIFlag,
                                UIDisplayOrder = i.UIDisplayOrder,
                                UIName = ResolveHash(adf, i.UIName),
                                UIDescription = ResolveHash(adf, i.UIDescription),
                                UITypeDescription = ResolveHash(adf, i.UITypeDescription),
                                UIFlagDescription = ResolveHash(adf, i.UIFlagDescription),
                                UIImagePath = i.UIImagePath,
                                UIVideoPath = i.UIVideoPath,
                                UIPar0 = i.UIPar0,
                                UIPar1 = i.UIPar1,
                                UIPar2 = i.UIPar2,
                                UIPar3 = i.UIPar3,
                                UIPar4 = i.UIPar4,
                            };
                            jsonItem.Feats.AddRange(i.Feats.Select(f => ResolveHash(adf, f)));
                            return jsonItem;
                        }).ToArray();

                    var serializer = JsonSerializer.Create();
                    serializer.Serialize(writer, jsonItems);
                }
            }
            else if (mode == Mode.Import)
            {
                string inputPath = extras[0];
                string outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputPath, ".onlinec");

                var adf = new AdfFile()
                {
                    Endian = Endian.Little,
                };
                var itemRoot = new ItemRoot();

                using (var input = File.OpenRead(inputPath))
                using (var textReader = new StreamReader(input))
                using (var reader = new JsonTextReader(textReader))
                {
                    var serializer = JsonSerializer.Create();
                    var jsonItems = serializer.Deserialize<ItemRootJson.Item[]>(reader);

                    itemRoot.Items.AddRange(jsonItems.Select(
                        ji => new ItemRoot.Item()
                        {
                            Name = ComputeHash(adf, ji.Name),
                            BaseName = ComputeHash(adf, ji.BaseName),
                            Type = ji.Type,
                            Purchasable = ji.Purchasable,
                            Consumable = ji.Consumable,
                            Rewardable = ji.Rewardable,
                            Collectable = ji.Collectable,
                            Feats = ji.Feats.Select(f => ComputeHash(adf, f)).ToArray(),
                            Region = ComputeHash(adf, ji.Region),
                            RebelDropTimer = ji.RebelDropTimer,
                            MaxInventory = ji.MaxInventory,
                            UIType = (int)ji.UIType,
                            UIFlag = (int)ji.UIFlag,
                            UIDisplayOrder = ji.UIDisplayOrder,
                            UIName = ComputeHash(adf, ji.UIName),
                            UIDescription = ComputeHash(adf, ji.UIDescription),
                            UITypeDescription = ComputeHash(adf, ji.UITypeDescription),
                            UIFlagDescription = ComputeHash(adf, ji.UIFlagDescription),
                            UIImagePath = ji.UIImagePath,
                            UIVideoPath = ji.UIVideoPath,
                            UIPar0 = ji.UIPar0,
                            UIPar1 = ji.UIPar1,
                            UIPar2 = ji.UIPar2,
                            UIPar3 = ji.UIPar3,
                            UIPar4 = ji.UIPar4,
                        }));
                }

                using (var output = File.Create(outputPath))
                {
                    using (var data = new MemoryStream())
                    {
                        itemRoot.Serialize(data, adf.Endian);
                        data.Flush();
                        data.Position = 0;

                        output.Position = adf.EstimateHeaderSize();
                        var itemRootPosition = output.Position;
                        output.WriteFromStream(data, data.Length);

                        adf.InstanceInfos.Add(new AdfFile.InstanceInfo()
                        {
                            Name = "Profile",
                            NameHash = "Profile".HashJenkins(),
                            Offset = (uint)itemRootPosition,
                            Size = (uint)data.Length,
                            TypeHash = ItemRoot.TypeHash,
                        });

                        adf.Serialize(output, 0);
                    }
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static uint ComputeHash(AdfFile adf, string value)
        {
            if (string.IsNullOrEmpty(value) == true)
            {
                return 0;
            }

            uint valueHash;
            if (value[0] == '#')
            {
                if (uint.TryParse(value.Substring(1),
                                  NumberStyles.AllowHexSpecifier,
                                  CultureInfo.InvariantCulture,
                                  out valueHash) == false)
                {
                    throw new FormatException();
                }
            }
            else
            {
                valueHash = value.HashJenkins();
                if (valueHash != 0 && adf.StringHashInfos.Any(shi => shi.ValueHash == valueHash) == false)
                {
                    adf.StringHashInfos.Add(new AdfFile.StringHashInfo()
                    {
                        Value = value,
                        ValueHash = valueHash,
                    });
                }
            }
            return valueHash;
        }

        private static string ResolveHash(AdfFile adf, uint valueHash)
        {
            if (valueHash == 0)
            {
                return "";
            }
            var stringHashInfo = adf.StringHashInfos.FirstOrDefault(shi => shi.ValueHash == valueHash);
            return stringHashInfo == default(FileFormats.AdfFile.StringHashInfo)
                       ? string.Format(CultureInfo.InvariantCulture, "#{0:X8}", valueHash)
                       : stringHashInfo.Value;
        }
    }
}
