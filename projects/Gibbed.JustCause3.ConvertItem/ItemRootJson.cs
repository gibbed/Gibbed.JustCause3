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
using Newtonsoft.Json.Converters;

namespace Gibbed.JustCause3.ConvertItem
{
    internal class ItemRootJson
    {
        [JsonObject(MemberSerialization.OptIn)]
        public class Item
        {
            #region Fields
            private string _Name;
            private string _BaseName;
            private int _Type;
            private int _Purchasable;
            private int _Consumable;
            private int _Rewardable;
            private int _Collectable;
            private readonly List<string> _Feats;
            private string _Region;
            private int _RebelDropTimer;
            private int _MaxInventory;
            private UIItemType _UIType;
            private UIItemSubtype _UIFlag;
            private int _UIDisplayOrder;
            private string _UIName;
            private string _UIDescription;
            private string _UITypeDescription;
            private string _UIFlagDescription;
            private string _UIImagePath;
            private string _UIVideoPath;
            private float _UIPar0;
            private float _UIPar1;
            private float _UIPar2;
            private float _UIPar3;
            private float _UIPar4;
            #endregion

            public Item()
            {
                this._Feats = new List<string>();
            }

            #region Properties
            [JsonProperty("name")]
            public string Name
            {
                get { return this._Name; }
                set { this._Name = value; }
            }

            [JsonProperty("base_name")]
            public string BaseName
            {
                get { return this._BaseName; }
                set { this._BaseName = value; }
            }

            [JsonProperty("type")]
            public int Type
            {
                get { return this._Type; }
                set { this._Type = value; }
            }

            [JsonProperty("purchasable")]
            public int Purchasable
            {
                get { return this._Purchasable; }
                set { this._Purchasable = value; }
            }

            [JsonProperty("consumable")]
            public int Consumable
            {
                get { return this._Consumable; }
                set { this._Consumable = value; }
            }

            [JsonProperty("rewardable")]
            public int Rewardable
            {
                get { return this._Rewardable; }
                set { this._Rewardable = value; }
            }

            [JsonProperty("collectable")]
            public int Collectable
            {
                get { return this._Collectable; }
                set { this._Collectable = value; }
            }

            [JsonProperty("feats")]
            public List<string> Feats
            {
                get { return this._Feats; }
            }

            [JsonProperty("region")]
            public string Region
            {
                get { return this._Region; }
                set { this._Region = value; }
            }

            [JsonProperty("rebel_drop_timer")]
            public int RebelDropTimer
            {
                get { return this._RebelDropTimer; }
                set { this._RebelDropTimer = value; }
            }

            [JsonProperty("max_inventory")]
            public int MaxInventory
            {
                get { return this._MaxInventory; }
                set { this._MaxInventory = value; }
            }

            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty("ui_type")]
            public UIItemType UIType
            {
                get { return this._UIType; }
                set { this._UIType = value; }
            }

            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty("ui_flag")]
            public UIItemSubtype UIFlag
            {
                get { return this._UIFlag; }
                set { this._UIFlag = value; }
            }

            [JsonProperty("ui_display_order")]
            public int UIDisplayOrder
            {
                get { return this._UIDisplayOrder; }
                set { this._UIDisplayOrder = value; }
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

            [JsonProperty("ui_type_description")]
            public string UITypeDescription
            {
                get { return this._UITypeDescription; }
                set { this._UITypeDescription = value; }
            }

            [JsonProperty("ui_flag_description")]
            public string UIFlagDescription
            {
                get { return this._UIFlagDescription; }
                set { this._UIFlagDescription = value; }
            }

            [JsonProperty("ui_image_path")]
            public string UIImagePath
            {
                get { return this._UIImagePath; }
                set { this._UIImagePath = value; }
            }

            [JsonProperty("ui_video_path")]
            public string UIVideoPath
            {
                get { return this._UIVideoPath; }
                set { this._UIVideoPath = value; }
            }

            [JsonProperty("ui_par_0")]
            public float UIPar0
            {
                get { return this._UIPar0; }
                set { this._UIPar0 = value; }
            }

            [JsonProperty("ui_par_1")]
            public float UIPar1
            {
                get { return this._UIPar1; }
                set { this._UIPar1 = value; }
            }

            [JsonProperty("ui_par_2")]
            public float UIPar2
            {
                get { return this._UIPar2; }
                set { this._UIPar2 = value; }
            }

            [JsonProperty("ui_par_3")]
            public float UIPar3
            {
                get { return this._UIPar3; }
                set { this._UIPar3 = value; }
            }

            [JsonProperty("ui_par_4")]
            public float UIPar4
            {
                get { return this._UIPar4; }
                set { this._UIPar4 = value; }
            }
            #endregion
        }
    }
}
