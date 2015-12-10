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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Gibbed.JustCause3.ConvertTask
{
    internal static class TaskRootJson
    {
        [JsonObject(MemberSerialization.OptIn)]
        public class Task
        {
            #region Fields
            private string _Name;
            private string _TaskGroup;
            private int _Type;
            private int _ProvinceWeight;
            private int _LiberationPercent;
            private int _RepeatableRewardSemantics;
            private Reward _RepeatableReward;
            private readonly List<RewardThreshold> _RewardThresholds;
            private readonly List<Prerequisite> _Prerequisites;
            private int _HasChurch;
            private int _ShownOnMap;
            private string _UIName;
            private string _UIDescription;
            private string _UITip;
            private int _UIOrderHint;
            #endregion

            public Task()
            {
                this._RewardThresholds = new List<RewardThreshold>();
                this._Prerequisites = new List<Prerequisite>();
            }

            #region Properties
            [JsonProperty("name")]
            public string Name
            {
                get { return this._Name; }
                set { this._Name = value; }
            }

            [JsonProperty("task_group")]
            public string TaskGroup
            {
                get { return this._TaskGroup; }
                set { this._TaskGroup = value; }
            }

            [JsonProperty("type")]
            public int Type
            {
                get { return this._Type; }
                set { this._Type = value; }
            }

            [JsonProperty("province_weight")]
            public int ProvinceWeight
            {
                get { return this._ProvinceWeight; }
                set { this._ProvinceWeight = value; }
            }

            [JsonProperty("liberation_percent")]
            public int LiberationPercent
            {
                get { return this._LiberationPercent; }
                set { this._LiberationPercent = value; }
            }

            [JsonProperty("repeatable_reward_semantics")]
            public int RepeatableRewardSemantics
            {
                get { return this._RepeatableRewardSemantics; }
                set { this._RepeatableRewardSemantics = value; }
            }

            [JsonProperty("repeatable_reward")]
            public Reward RepeatableReward
            {
                get { return this._RepeatableReward; }
                set { this._RepeatableReward = value; }
            }

            [JsonProperty("reward_thresholds")]
            public List<RewardThreshold> RewardThresholds
            {
                get { return this._RewardThresholds; }
            }

            [JsonProperty("prerequisites")]
            public List<Prerequisite> Prerequisites
            {
                get { return this._Prerequisites; }
            }

            [JsonProperty("has_church")]
            public int HasChurch
            {
                get { return this._HasChurch; }
                set { this._HasChurch = value; }
            }

            [JsonProperty("shown_on_map")]
            public int ShownOnMap
            {
                get { return this._ShownOnMap; }
                set { this._ShownOnMap = value; }
            }

            [JsonProperty("ui_name")]
            public string UIName
            {
                get { return this._UIName; }
                set { this._UIName = value; }
            }

            [JsonProperty("ui_description")]
            public string UIDescription
            {
                get { return this._UIDescription; }
                set { this._UIDescription = value; }
            }

            [JsonProperty("ui_tip")]
            public string UITip
            {
                get { return this._UITip; }
                set { this._UITip = value; }
            }

            [JsonProperty("ui_order_hint")]
            public int UIOrderHint
            {
                get { return this._UIOrderHint; }
                set { this._UIOrderHint = value; }
            }
            #endregion
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class Reward
        {
            #region Fields
            private int _Skillpoints;
            private int _Chaos;
            private int _ItemRewardSemantics;
            private readonly List<ItemReward> _Items;
            #endregion

            public Reward()
            {
                this._Items = new List<ItemReward>();
            }

            #region Properties
            [JsonProperty("skillpoints")]
            public int Skillpoints
            {
                get { return this._Skillpoints; }
                set { this._Skillpoints = value; }
            }

            [JsonProperty("chaos")]
            public int Chaos
            {
                get { return this._Chaos; }
                set { this._Chaos = value; }
            }

            [JsonProperty("item_reward_semantics")]
            public int ItemRewardSemantics
            {
                get { return this._ItemRewardSemantics; }
                set { this._ItemRewardSemantics = value; }
            }

            [JsonProperty("items")]
            public List<ItemReward> Items
            {
                get { return this._Items; }
            }
            #endregion
        }

        [JsonObject(MemberSerialization.OptIn)]
        public struct ItemReward
        {
            #region Fields
            private string _Item;
            private int _Permanent;
            private int _Duration;
            private int _Quantity;
            private int _Delivery;
            #endregion

            #region Properties
            [JsonProperty("item")]
            public string Item
            {
                get { return this._Item; }
                set { this._Item = value; }
            }

            [JsonProperty("permanent")]
            public int Permanent
            {
                get { return this._Permanent; }
                set { this._Permanent = value; }
            }

            [JsonProperty("duration")]
            public int Duration
            {
                get { return this._Duration; }
                set { this._Duration = value; }
            }

            [JsonProperty("quantity")]
            public int Quantity
            {
                get { return this._Quantity; }
                set { this._Quantity = value; }
            }

            [JsonProperty("delivery")]
            public int Delivery
            {
                get { return this._Delivery; }
                set { this._Delivery = value; }
            }
            #endregion
        }

        [JsonObject(MemberSerialization.OptIn)]
        public struct RewardThreshold
        {
            #region Fields
            private long _Threshold;
            private Reward _Reward;
            #endregion

            #region Properties
            [JsonProperty("threshold")]
            public long Threshold
            {
                get { return this._Threshold; }
                set { this._Threshold = value; }
            }

            [JsonProperty("reward")]
            public Reward Reward
            {
                get { return this._Reward; }
                set { this._Reward = value; }
            }
            #endregion
        }

        [JsonObject(MemberSerialization.OptIn)]
        public struct Prerequisite
        {
            #region Fields
            private string _Name;
            private int _RewardIndex;
            private string _Tag;
            #endregion

            #region Properties
            [JsonProperty("name")]
            public string Name
            {
                get { return this._Name; }
                set { this._Name = value; }
            }

            [JsonProperty("reward_index")]
            public int RewardIndex
            {
                get { return this._RewardIndex; }
                set { this._RewardIndex = value; }
            }

            [JsonProperty("tag")]
            public string Tag
            {
                get { return this._Tag; }
                set { this._Tag = value; }
            }
            #endregion
        }
    }
}
