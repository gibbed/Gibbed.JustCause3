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

namespace Gibbed.JustCause3.ConvertTask
{
    internal class TaskRoot
    {
        public const uint TypeHash = 0xBA99D54C;

        #region Fields
        private readonly List<Task> _Tasks;
        #endregion

        public TaskRoot()
        {
            this._Tasks = new List<Task>();
        }

        #region Properties
        public List<Task> Tasks
        {
            get { return this._Tasks; }
        }
        #endregion

        public void Serialize(Stream output, Endian endian)
        {
            var basePosition = output.Position;

            output.Seek(RawTaskRoot.Size, SeekOrigin.Current);
            var taskPosition = output.Position;

            output.Seek(RawTask.Size * this._Tasks.Count, SeekOrigin.Current);
            var rawTasks = new RawTask[this._Tasks.Count];
            for (int i = 0; i < this._Tasks.Count; i++)
            {
                var task = this._Tasks[i];
                rawTasks[i] = new RawTask()
                {
                    Name = task.Name,
                    TaskGroup = task.TaskGroup,
                    Type = task.Type,
                    ProvinceWeight = task.ProvinceWeight,
                    LiberationPercent = task.LiberationPercent,
                    RepeatableRewardSemantics = task.RepeatableRewardSemantics,
                    RepeatableReward = new RawReward()
                    {
                        Skillpoints = task.RepeatableReward.Skillpoints,
                        Chaos = task.RepeatableReward.Chaos,
                        ItemRewardSemantics = task.RepeatableReward.ItemRewardSemantics,
                        ItemCount = task.RepeatableReward.Items.Length,
                    },
                    RewardThresholdCount = task.RewardThresholds == null ? 0 : task.RewardThresholds.Length,
                    PrerequisiteCount = task.Prerequisites == null ? 0 : task.Prerequisites.Length,
                    HasChurch = task.HasChurch,
                    ShownOnMap = task.ShownOnMap,
                    UIName = task.UIName,
                    UIDescription = task.UIDescription,
                    UITip = task.UITip,
                    UIOrderHint = task.UIOrderHint,
                };
            }

            for (int i = 0; i < this._Tasks.Count; i++)
            {
                var task = this._Tasks[i];

                if (task.RepeatableReward.Items != null && task.RepeatableReward.Items.Length > 0)
                {
                    rawTasks[i].RepeatableReward.ItemOffset = output.Position - basePosition;
                    foreach (var item in task.RepeatableReward.Items)
                    {
                        item.Write(output, endian);
                    }
                }

                if (task.RewardThresholds != null && task.RewardThresholds.Length > 0)
                {
                    var rewardThresholdPosition = output.Position;
                    rawTasks[i].RewardThresholdOffset = output.Position - basePosition;

                    output.Seek(RawRewardThreshold.Size * task.RewardThresholds.Length, SeekOrigin.Current);
                    var rawRewardThresholds = new RawRewardThreshold[task.RewardThresholds.Length];
                    for (int j = 0; j < task.RewardThresholds.Length; j++)
                    {
                        var rewardThreshold = task.RewardThresholds[j];
                        var rawRewardThreshold = new RawRewardThreshold()
                        {
                            Threshold = rewardThreshold.Threshold,
                            Reward = new RawReward()
                            {
                                Skillpoints = rewardThreshold.Reward.Skillpoints,
                                Chaos = rewardThreshold.Reward.Chaos,
                                ItemRewardSemantics = rewardThreshold.Reward.ItemRewardSemantics,
                                ItemCount =
                                    rewardThreshold.Reward.Items == null ? 0 : rewardThreshold.Reward.Items.Length,
                            },
                        };

                        if (rewardThreshold.Reward.Items != null && rewardThreshold.Reward.Items.Length > 0)
                        {
                            rawRewardThreshold.Reward.ItemOffset = output.Position - basePosition;
                            foreach (var item in rewardThreshold.Reward.Items)
                            {
                                item.Write(output, endian);
                            }
                        }

                        rawRewardThresholds[j] = rawRewardThreshold;
                    }

                    var endPosition = output.Position;
                    output.Position = rewardThresholdPosition;
                    foreach (var rawRewardThreshold in rawRewardThresholds)
                    {
                        rawRewardThreshold.Write(output, endian);
                    }
                    output.Position = endPosition;
                }

                if (task.Prerequisites != null && task.Prerequisites.Length > 0)
                {
                    rawTasks[i].PrerequisiteOffset = output.Position - basePosition;
                    foreach (var prerequisite in task.Prerequisites)
                    {
                        prerequisite.Write(output, endian);
                    }
                }
            }

            output.Position = taskPosition;
            foreach (var rawTask in rawTasks)
            {
                rawTask.Write(output, endian);
            }

            output.Position = basePosition;
            new RawTaskRoot()
            {
                TaskOffset = taskPosition,
                TaskCount = this._Tasks.Count,
            }.Write(output, endian);
        }

        public void Deserialize(Stream input, Endian endian)
        {
            var basePosition = input.Position;

            var rawTaskRoot = RawTaskRoot.Read(input, endian);

            var tasks = new Task[rawTaskRoot.TaskCount];
            if (rawTaskRoot.TaskCount != 0)
            {
                if (rawTaskRoot.TaskCount < 0 || rawTaskRoot.TaskCount > int.MaxValue)
                {
                    throw new FormatException();
                }

                var rawTasks = new RawTask[rawTaskRoot.TaskCount];
                input.Position = basePosition + rawTaskRoot.TaskOffset;
                for (long i = 0; i < rawTaskRoot.TaskCount; i++)
                {
                    rawTasks[i] = RawTask.Read(input, endian);
                }

                for (long i = 0; i < rawTaskRoot.TaskCount; i++)
                {
                    var rawTask = rawTasks[i];

                    var task = new Task()
                    {
                        Name = rawTask.Name,
                        TaskGroup = rawTask.TaskGroup,
                        Type = rawTask.Type,
                        ProvinceWeight = rawTask.ProvinceWeight,
                        LiberationPercent = rawTask.LiberationPercent,
                        RepeatableRewardSemantics = rawTask.RepeatableRewardSemantics,
                        RepeatableReward = new Reward()
                        {
                            Skillpoints = rawTask.RepeatableReward.Skillpoints,
                            Chaos = rawTask.RepeatableReward.Chaos,
                            ItemRewardSemantics = rawTask.RepeatableReward.ItemRewardSemantics,
                            Items = new ItemReward[rawTask.RepeatableReward.ItemCount],
                        },
                        RewardThresholds = new RewardThreshold[rawTask.RewardThresholdCount],
                        Prerequisites = new Prerequisite[rawTask.PrerequisiteCount],
                        HasChurch = rawTask.HasChurch,
                        ShownOnMap = rawTask.ShownOnMap,
                        UIName = rawTask.UIName,
                        UIDescription = rawTask.UIDescription,
                        UITip = rawTask.UITip,
                        UIOrderHint = rawTask.UIOrderHint,
                    };

                    if (rawTask.RepeatableReward.ItemCount != 0)
                    {
                        if (rawTask.RepeatableReward.ItemCount < 0 || rawTask.RepeatableReward.ItemCount > int.MaxValue)
                        {
                            throw new FormatException();
                        }

                        input.Position = basePosition + rawTask.RepeatableReward.ItemOffset;
                        for (long j = 0; j < rawTask.RepeatableReward.ItemCount; j++)
                        {
                            task.RepeatableReward.Items[j] = ItemReward.Read(input, endian);
                        }
                    }

                    if (rawTask.RewardThresholdCount != 0)
                    {
                        if (rawTask.RewardThresholdCount < 0 || rawTask.RewardThresholdCount > int.MaxValue)
                        {
                            throw new FormatException();
                        }

                        var rawRewardThresholds = new RawRewardThreshold[rawTask.RewardThresholdCount];
                        input.Position = basePosition + rawTask.RewardThresholdOffset;
                        for (long j = 0; j < rawTask.RewardThresholdCount; j++)
                        {
                            rawRewardThresholds[j] = RawRewardThreshold.Read(input, endian);
                        }

                        for (long j = 0; j < rawTask.RewardThresholdCount; j++)
                        {
                            var rawRewardThreshold = rawRewardThresholds[j];
                            var rewardThreshold = new RewardThreshold()
                            {
                                Threshold = rawRewardThreshold.Threshold,
                                Reward = new Reward()
                                {
                                    Skillpoints = rawRewardThreshold.Reward.Skillpoints,
                                    Chaos = rawRewardThreshold.Reward.Chaos,
                                    ItemRewardSemantics = rawRewardThreshold.Reward.ItemRewardSemantics,
                                    Items = new ItemReward[rawRewardThreshold.Reward.ItemCount],
                                },
                            };

                            if (rawRewardThreshold.Reward.ItemCount != 0)
                            {
                                if (rawRewardThreshold.Reward.ItemCount < 0 ||
                                    rawRewardThreshold.Reward.ItemCount > int.MaxValue)
                                {
                                    throw new FormatException();
                                }

                                input.Position = basePosition + rawRewardThreshold.Reward.ItemOffset;
                                for (long k = 0; k < rawRewardThreshold.Reward.ItemCount; k++)
                                {
                                    rewardThreshold.Reward.Items[k] = ItemReward.Read(input, endian);
                                }
                            }

                            task.RewardThresholds[j] = rewardThreshold;
                        }
                    }

                    if (rawTask.PrerequisiteCount != 0)
                    {
                        if (rawTask.PrerequisiteCount < 0 || rawTask.PrerequisiteCount > int.MaxValue)
                        {
                            throw new FormatException();
                        }

                        input.Position = basePosition + rawTask.PrerequisiteOffset;
                        for (long j = 0; j < rawTask.PrerequisiteCount; j++)
                        {
                            task.Prerequisites[j] = Prerequisite.Read(input, endian);
                        }
                    }

                    tasks[i] = task;
                }
            }

            this._Tasks.Clear();
            this._Tasks.AddRange(tasks);
        }

        private struct RawTaskRoot
        {
            public const int Size = 16;

            public long TaskOffset;
            public long TaskCount;

            public static RawTaskRoot Read(Stream input, Endian endian)
            {
                var instance = new RawTaskRoot();
                instance.TaskOffset = input.ReadValueS64(endian);
                instance.TaskCount = input.ReadValueS64(endian);
                return instance;
            }

            public static void Write(Stream output, RawTaskRoot instance, Endian endian)
            {
                output.WriteValueS64(instance.TaskOffset, endian);
                output.WriteValueS64(instance.TaskCount, endian);
            }

            public void Write(Stream output, Endian endian)
            {
                Write(output, this, endian);
            }
        }

        private struct RawTask
        {
            public const int Size = 112;

            public uint Name;
            public uint TaskGroup;
            public int Type;
            public int ProvinceWeight;
            public int LiberationPercent;
            public int RepeatableRewardSemantics;
            public RawReward RepeatableReward;
            public long RewardThresholdOffset;
            public long RewardThresholdCount;
            public long PrerequisiteCount;
            public long PrerequisiteOffset;
            public int HasChurch;
            public int ShownOnMap;
            public uint UIName;
            public uint UIDescription;
            public uint UITip;
            public int UIOrderHint;

            public static RawTask Read(Stream input, Endian endian)
            {
                var instance = new RawTask();
                instance.Name = input.ReadValueU32(endian);
                instance.TaskGroup = input.ReadValueU32(endian);
                instance.Type = input.ReadValueS32(endian);
                instance.ProvinceWeight = input.ReadValueS32(endian);
                instance.LiberationPercent = input.ReadValueS32(endian);
                instance.RepeatableRewardSemantics = input.ReadValueS32(endian);
                instance.RepeatableReward = RawReward.Read(input, endian);
                instance.RewardThresholdOffset = input.ReadValueS64(endian);
                instance.RewardThresholdCount = input.ReadValueS64(endian);
                instance.PrerequisiteOffset = input.ReadValueS64(endian);
                instance.PrerequisiteCount = input.ReadValueS64(endian);
                instance.HasChurch = input.ReadValueS32(endian);
                instance.ShownOnMap = input.ReadValueS32(endian);
                instance.UIName = input.ReadValueU32(endian);
                instance.UIDescription = input.ReadValueU32(endian);
                instance.UITip = input.ReadValueU32(endian);
                instance.UIOrderHint = input.ReadValueS32(endian);
                return instance;
            }

            public static void Write(Stream output, RawTask instance, Endian endian)
            {
                output.WriteValueU32(instance.Name, endian);
                output.WriteValueU32(instance.TaskGroup, endian);
                output.WriteValueS32(instance.Type, endian);
                output.WriteValueS32(instance.ProvinceWeight, endian);
                output.WriteValueS32(instance.LiberationPercent, endian);
                output.WriteValueS32(instance.RepeatableRewardSemantics, endian);
                instance.RepeatableReward.Write(output, endian);
                output.WriteValueS64(instance.RewardThresholdOffset, endian);
                output.WriteValueS64(instance.RewardThresholdCount, endian);
                output.WriteValueS64(instance.PrerequisiteOffset, endian);
                output.WriteValueS64(instance.PrerequisiteCount, endian);
                output.WriteValueS32(instance.HasChurch, endian);
                output.WriteValueS32(instance.ShownOnMap, endian);
                output.WriteValueU32(instance.UIName, endian);
                output.WriteValueU32(instance.UIDescription, endian);
                output.WriteValueU32(instance.UITip, endian);
                output.WriteValueS32(instance.UIOrderHint, endian);
            }

            public void Write(Stream output, Endian endian)
            {
                Write(output, this, endian);
            }
        }

        private struct RawReward
        {
            public const int Size = 32;

            public int Skillpoints;
            public int Chaos;
            public int ItemRewardSemantics;
            public long ItemOffset;
            public long ItemCount;

            public static RawReward Read(Stream input, Endian endian)
            {
                var instance = new RawReward();
                instance.Skillpoints = input.ReadValueS32(endian);
                instance.Chaos = input.ReadValueS32(endian);
                instance.ItemRewardSemantics = input.ReadValueS32(endian);
                input.Seek(4, SeekOrigin.Current);
                instance.ItemOffset = input.ReadValueS64(endian);
                instance.ItemCount = input.ReadValueS64(endian);
                return instance;
            }

            public static void Write(Stream output, RawReward instance, Endian endian)
            {
                output.WriteValueS32(instance.Skillpoints, endian);
                output.WriteValueS32(instance.Chaos, endian);
                output.WriteValueS32(instance.ItemRewardSemantics, endian);
                output.Seek(4, SeekOrigin.Current);
                output.WriteValueS64(instance.ItemOffset, endian);
                output.WriteValueS64(instance.ItemCount, endian);
            }

            public void Write(Stream output, Endian endian)
            {
                Write(output, this, endian);
            }
        }

        public struct ItemReward
        {
            internal const int Size = 20;

            public uint Item;
            public int Permanent;
            public int Duration;
            public int Quantity;
            public int Delivery;

            internal static ItemReward Read(Stream input, Endian endian)
            {
                var instance = new ItemReward();
                instance.Item = input.ReadValueU32(endian);
                instance.Permanent = input.ReadValueS32(endian);
                instance.Duration = input.ReadValueS32(endian);
                instance.Quantity = input.ReadValueS32(endian);
                instance.Delivery = input.ReadValueS32(endian);
                return instance;
            }

            internal static void Write(Stream output, ItemReward instance, Endian endian)
            {
                output.WriteValueU32(instance.Item, endian);
                output.WriteValueS32(instance.Permanent, endian);
                output.WriteValueS32(instance.Duration, endian);
                output.WriteValueS32(instance.Quantity, endian);
                output.WriteValueS32(instance.Delivery, endian);
            }

            internal void Write(Stream output, Endian endian)
            {
                Write(output, this, endian);
            }
        }

        private struct RawRewardThreshold
        {
            public const int Size = 40;

            public long Threshold;
            public RawReward Reward;

            public static RawRewardThreshold Read(Stream input, Endian endian)
            {
                var instance = new RawRewardThreshold();
                instance.Threshold = input.ReadValueS64(endian);
                instance.Reward = RawReward.Read(input, endian);
                return instance;
            }

            public static void Write(Stream output, RawRewardThreshold instance, Endian endian)
            {
                output.WriteValueS64(instance.Threshold, endian);
                instance.Reward.Write(output, endian);
            }

            public void Write(Stream output, Endian endian)
            {
                Write(output, this, endian);
            }
        }

        public struct Prerequisite
        {
            public const int Size = 12;

            public uint Name;
            public int RewardIndex;
            public uint Tag;

            internal static Prerequisite Read(Stream input, Endian endian)
            {
                var instance = new Prerequisite();
                instance.Name = input.ReadValueU32(endian);
                instance.RewardIndex = input.ReadValueS32(endian);
                instance.Tag = input.ReadValueU32(endian);
                return instance;
            }

            internal static void Write(Stream output, Prerequisite instance, Endian endian)
            {
                output.WriteValueU32(instance.Name, endian);
                output.WriteValueS32(instance.RewardIndex, endian);
                output.WriteValueU32(instance.Tag, endian);
            }

            internal void Write(Stream output, Endian endian)
            {
                Write(output, this, endian);
            }
        }

        public struct Task
        {
            public uint Name;
            public uint TaskGroup;
            public int Type;
            public int ProvinceWeight;
            public int LiberationPercent;
            public int RepeatableRewardSemantics;
            public Reward RepeatableReward;
            public RewardThreshold[] RewardThresholds;
            public Prerequisite[] Prerequisites;
            public int HasChurch;
            public int ShownOnMap;
            public uint UIName;
            public uint UIDescription;
            public uint UITip;
            public int UIOrderHint;
        }

        public struct Reward
        {
            public int Skillpoints;
            public int Chaos;
            public int ItemRewardSemantics;
            public ItemReward[] Items;
        }

        public struct RewardThreshold
        {
            public long Threshold;
            public Reward Reward;
        }
    }
}
