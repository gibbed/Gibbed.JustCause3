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

namespace Gibbed.JustCause3.ConvertTask
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
                Console.WriteLine("Usage: {0} [OPTIONS]+ [-e] task.onlinec [output_json]", GetExecutableName());
                Console.WriteLine("       {0} [OPTIONS]+ [-i] input_json [task.onlinec]", GetExecutableName());
                Console.WriteLine("Convert an task file between binary and JSON format.");
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
                var taskRoot = new TaskRoot();

                using (var input = File.OpenRead(inputPath))
                {
                    adf.Deserialize(input);

                    var taskRootInfo = adf.InstanceInfos.FirstOrDefault(i => i.Name == "Profile");
                    if (taskRootInfo.TypeHash != TaskRoot.TypeHash)
                    {
                        throw new FormatException();
                    }

                    input.Position = taskRootInfo.Offset;
                    taskRoot.Deserialize(input, adf.Endian);
                }

                using (var output = File.Create(outputPath))
                using (var textWriter = new StreamWriter(output))
                using (var writer = new JsonTextWriter(textWriter))
                {
                    writer.Indentation = 2;
                    writer.IndentChar = ' ';
                    writer.Formatting = Formatting.Indented;

                    var jsonTasks = taskRoot.Tasks.Select(
                        t =>
                        {
                            var jsonTask = new TaskRootJson.Task()
                            {
                                Name = ResolveHash(adf, t.Name),
                                TaskGroup = ResolveHash(adf, t.TaskGroup),
                                Type = t.Type,
                                ProvinceWeight = t.ProvinceWeight,
                                LiberationPercent = t.LiberationPercent,
                                RepeatableRewardSemantics = t.RepeatableRewardSemantics,
                                RepeatableReward = new TaskRootJson.Reward()
                                {
                                    Skillpoints = t.RepeatableReward.Skillpoints,
                                    Chaos = t.RepeatableReward.Chaos,
                                    ItemRewardSemantics = t.RepeatableReward.ItemRewardSemantics,
                                },
                                HasChurch = t.HasChurch,
                                ShownOnMap = t.ShownOnMap,
                                UIName = ResolveHash(adf, t.UIName),
                                UIDescription = ResolveHash(adf, t.UIDescription),
                                UITip = ResolveHash(adf, t.UITip),
                                UIOrderHint = t.UIOrderHint,
                            };
                            if (t.RepeatableReward.Items != null)
                            {
                                jsonTask.RepeatableReward.Items.AddRange(
                                    t.RepeatableReward.Items.Select(i => new TaskRootJson.ItemReward()
                                    {
                                        Item = ResolveHash(adf, i.Item),
                                        Permanent = i.Permanent,
                                        Duration = i.Duration,
                                        Quantity = i.Quantity,
                                        Delivery = i.Delivery,
                                    }));
                            }
                            if (t.RewardThresholds != null)
                            {
                                jsonTask.RewardThresholds.AddRange(
                                    t.RewardThresholds.Select(
                                        rt =>
                                        {
                                            var jsonRewardThreshold = new TaskRootJson.RewardThreshold()
                                            {
                                                Threshold = rt.Threshold,
                                                Reward = new TaskRootJson.Reward()
                                                {
                                                    Skillpoints = rt.Reward.Skillpoints,
                                                    Chaos = rt.Reward.Chaos,
                                                    ItemRewardSemantics = rt.Reward.ItemRewardSemantics,
                                                },
                                            };
                                            if (rt.Reward.Items != null)
                                            {
                                                jsonRewardThreshold.Reward.Items.AddRange(
                                                    rt.Reward.Items.Select(i => new TaskRootJson.ItemReward()
                                                    {
                                                        Item = ResolveHash(adf, i.Item),
                                                        Permanent = i.Permanent,
                                                        Duration = i.Duration,
                                                        Quantity = i.Quantity,
                                                        Delivery = i.Delivery,
                                                    }));
                                            }
                                            return jsonRewardThreshold;
                                        }));
                            }
                            if (t.Prerequisites != null)
                            {
                                jsonTask.Prerequisites.AddRange(
                                    t.Prerequisites.Select(p => new TaskRootJson.Prerequisite()
                                    {
                                        Name = ResolveHash(adf, p.Name),
                                        RewardIndex = p.RewardIndex,
                                        Tag = ResolveHash(adf, p.Tag),
                                    }));
                            }
                            return jsonTask;
                        }).ToArray();

                    var serializer = JsonSerializer.Create();
                    serializer.Serialize(writer, jsonTasks);
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
                var taskRoot = new TaskRoot();

                using (var input = File.OpenRead(inputPath))
                using (var textReader = new StreamReader(input))
                using (var reader = new JsonTextReader(textReader))
                {
                    var serializer = JsonSerializer.Create();
                    var jsonTasks = serializer.Deserialize<TaskRootJson.Task[]>(reader);

                    taskRoot.Tasks.AddRange(jsonTasks.Select(
                        jt =>
                        {
                            var task = new TaskRoot.Task()
                            {
                                Name = ComputeHash(adf, jt.Name),
                                TaskGroup = ComputeHash(adf, jt.TaskGroup),
                                Type = jt.Type,
                                ProvinceWeight = jt.ProvinceWeight,
                                LiberationPercent = jt.LiberationPercent,
                                RepeatableRewardSemantics = jt.RepeatableRewardSemantics,
                                RepeatableReward = new TaskRoot.Reward()
                                {
                                    Skillpoints = jt.RepeatableReward.Skillpoints,
                                    Chaos = jt.RepeatableReward.Chaos,
                                    ItemRewardSemantics = jt.RepeatableReward.ItemRewardSemantics,
                                },
                                HasChurch = jt.HasChurch,
                                ShownOnMap = jt.ShownOnMap,
                                UIName = ComputeHash(adf, jt.UIName),
                                UIDescription = ComputeHash(adf, jt.UIDescription),
                                UITip = ComputeHash(adf, jt.UITip),
                                UIOrderHint = jt.UIOrderHint,
                            };
                            task.RepeatableReward.Items =
                                jt.RepeatableReward.Items.Select(ji => new TaskRoot.ItemReward()
                                {
                                    Item = ComputeHash(adf, ji.Item),
                                    Permanent = ji.Permanent,
                                    Duration = ji.Duration,
                                    Quantity = ji.Quantity,
                                    Delivery = ji.Delivery,
                                }).ToArray();
                            task.RewardThresholds =
                                jt.RewardThresholds.Select(
                                    jrt =>
                                    {
                                        var rewardThreshold = new TaskRoot.RewardThreshold()
                                        {
                                            Threshold = jrt.Threshold,
                                            Reward = new TaskRoot.Reward()
                                            {
                                                Skillpoints = jrt.Reward.Skillpoints,
                                                Chaos = jrt.Reward.Chaos,
                                                ItemRewardSemantics = jrt.Reward.ItemRewardSemantics,
                                            },
                                        };
                                        rewardThreshold.Reward.Items =
                                            jrt.Reward.Items.Select(ji => new TaskRoot.ItemReward()
                                            {
                                                Item = ComputeHash(adf, ji.Item),
                                                Permanent = ji.Permanent,
                                                Duration = ji.Duration,
                                                Quantity = ji.Quantity,
                                                Delivery = ji.Delivery,
                                            }).ToArray();
                                        return rewardThreshold;
                                    }).ToArray();
                            task.Prerequisites =
                                jt.Prerequisites.Select(jp => new TaskRoot.Prerequisite()
                                {
                                    Name = ComputeHash(adf, jp.Name),
                                    RewardIndex = jp.RewardIndex,
                                    Tag = ComputeHash(adf, jp.Tag),
                                }).ToArray();
                            return task;
                        }));
                }

                using (var output = File.Create(outputPath))
                {
                    using (var data = new MemoryStream())
                    {
                        taskRoot.Serialize(data, adf.Endian);
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
                            TypeHash = TaskRoot.TypeHash,
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
