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
using System.Text;
using Gibbed.IO;

namespace Gibbed.JustCause3.ConvertItem
{
    public class ItemRoot
    {
        public const uint TypeHash = 0x79DE48AE;

        #region Fields
        private readonly List<Item> _Items;
        #endregion

        public ItemRoot()
        {
            this._Items = new List<Item>();
        }

        #region Properties
        public List<Item> Items
        {
            get { return this._Items; }
        }
        #endregion

        public void Serialize(Stream output, Endian endian)
        {
            var basePosition = output.Position;

            output.Seek(RawItemRoot.Size, SeekOrigin.Current);
            var itemPosition = output.Position;

            output.Seek(RawItem.Size * this._Items.Count, SeekOrigin.Current);
            var rawItems = new RawItem[this._Items.Count];
            for (int i = 0; i < this._Items.Count; i++)
            {
                var item = this._Items[i];
                rawItems[i] = new RawItem()
                {
                    Name = item.Name,
                    BaseName = item.BaseName,
                    Type = item.Type,
                    Purchasable = item.Purchasable,
                    Consumable = item.Consumable,
                    Rewardable = item.Rewardable,
                    Collectable = item.Collectable,
                    FeatCount = item.Feats == null ? 0 : item.Feats.Length,
                    Region = item.Region,
                    RebelDropTimer = item.RebelDropTimer,
                    MaxInventory = item.MaxInventory,
                    UIType = item.UIType,
                    UIFlag = item.UIFlag,
                    UIDisplayOrder = item.UIDisplayOrder,
                    UIName = item.UIName,
                    UIDescription = item.UIDescription,
                    UITypeDescription = item.UITypeDescription,
                    UIFlagDescription = item.UIFlagDescription,
                    UIPar0 = item.UIPar0,
                    UIPar1 = item.UIPar1,
                    UIPar2 = item.UIPar2,
                    UIPar3 = item.UIPar3,
                    UIPar4 = item.UIPar4,
                };
            }

            for (int i = 0; i < this._Items.Count; i++)
            {
                var item = this._Items[i];
                if (item.Feats != null && item.Feats.Length > 0)
                {
                    rawItems[i].FeatOffset = output.Position - basePosition;
                    foreach (var feat in item.Feats)
                    {
                        output.WriteValueU32(feat, endian);
                    }
                }
            }

            for (int i = 0; i < this._Items.Count; i++)
            {
                var item = this._Items[i];

                if (item.UIImagePath != null)
                {
                    //output.Position = output.Position.Align(8);
                    rawItems[i].UIImagePathOffset = output.Position - basePosition;
                    output.WriteStringZ(item.UIImagePath, Encoding.ASCII);
                }

                if (item.UIVideoPath != null)
                {
                    //output.Position = output.Position.Align(8);
                    rawItems[i].UIVideoPathOffset = output.Position - basePosition;
                    output.WriteStringZ(item.UIVideoPath, Encoding.ASCII);
                }
            }

            output.Position = itemPosition;
            foreach (var rawItem in rawItems)
            {
                rawItem.Write(output, endian);
            }

            output.Position = basePosition;
            new RawItemRoot()
            {
                ItemOffset = itemPosition,
                ItemCount = this._Items.Count,
            }.Write(output, endian);
        }

        public void Deserialize(Stream input, Endian endian)
        {
            var basePosition = input.Position;

            var rawItemRoot = RawItemRoot.Read(input, endian);

            var items = new Item[rawItemRoot.ItemCount];
            if (rawItemRoot.ItemCount != 0)
            {
                if (rawItemRoot.ItemCount < 0 || rawItemRoot.ItemCount > int.MaxValue)
                {
                    throw new FormatException();
                }

                var rawItems = new RawItem[rawItemRoot.ItemCount];
                input.Position = basePosition + rawItemRoot.ItemOffset;
                for (long i = 0; i < rawItemRoot.ItemCount; i++)
                {
                    rawItems[i] = RawItem.Read(input, endian);
                }

                for (long i = 0; i < rawItemRoot.ItemCount; i++)
                {
                    var rawItem = rawItems[i];
                    var item = new Item()
                    {
                        Name = rawItem.Name,
                        BaseName = rawItem.BaseName,
                        Type = rawItem.Type,
                        Purchasable = rawItem.Purchasable,
                        Consumable = rawItem.Consumable,
                        Rewardable = rawItem.Rewardable,
                        Collectable = rawItem.Collectable,
                        Feats = new uint[rawItem.FeatCount],
                        Region = rawItem.Region,
                        RebelDropTimer = rawItem.RebelDropTimer,
                        MaxInventory = rawItem.MaxInventory,
                        UIType = rawItem.UIType,
                        UIFlag = rawItem.UIFlag,
                        UIDisplayOrder = rawItem.UIDisplayOrder,
                        UIName = rawItem.UIName,
                        UIDescription = rawItem.UIDescription,
                        UITypeDescription = rawItem.UITypeDescription,
                        UIFlagDescription = rawItem.UIFlagDescription,
                        UIPar0 = rawItem.UIPar0,
                        UIPar1 = rawItem.UIPar1,
                        UIPar2 = rawItem.UIPar2,
                        UIPar3 = rawItem.UIPar3,
                        UIPar4 = rawItem.UIPar4,
                    };

                    if (rawItem.FeatCount != 0)
                    {
                        if (rawItem.FeatCount < 0 || rawItem.FeatCount > int.MaxValue)
                        {
                            throw new FormatException();
                        }

                        input.Position = basePosition + rawItem.FeatOffset;
                        for (long j = 0; j < rawItem.FeatCount; j++)
                        {
                            item.Feats[j] = input.ReadValueU32(endian);
                        }
                    }

                    if (rawItem.UIImagePathOffset != 0)
                    {
                        input.Position = basePosition + rawItem.UIImagePathOffset;
                        item.UIImagePath = input.ReadStringZ(Encoding.ASCII);
                    }

                    if (rawItem.UIVideoPathOffset != 0)
                    {
                        input.Position = basePosition + rawItem.UIImagePathOffset;
                        item.UIVideoPath = input.ReadStringZ(Encoding.ASCII);
                    }

                    items[i] = item;
                }
            }

            this._Items.Clear();
            this._Items.AddRange(items);
        }

        private struct RawItemRoot
        {
            public const int Size = 16;

            public long ItemOffset;
            public long ItemCount;

            public static RawItemRoot Read(Stream input, Endian endian)
            {
                var instance = new RawItemRoot();
                instance.ItemOffset = input.ReadValueS64(endian);
                instance.ItemCount = input.ReadValueS64(endian);
                return instance;
            }

            public static void Write(Stream output, RawItemRoot instance, Endian endian)
            {
                output.WriteValueS64(instance.ItemOffset, endian);
                output.WriteValueS64(instance.ItemCount, endian);
            }

            public void Write(Stream output, Endian endian)
            {
                Write(output, this, endian);
            }
        }

        private struct RawItem
        {
            public const int Size = 128;

            public uint Name;
            public uint BaseName;
            public int Type;
            public int Purchasable;
            public int Consumable;
            public int Rewardable;
            public int Collectable;
            public long FeatOffset;
            public long FeatCount;
            public uint Region;
            public int RebelDropTimer;
            public int MaxInventory;
            public int UIType;
            public int UIFlag;
            public int UIDisplayOrder;
            public uint UIName;
            public uint UIDescription;
            public uint UITypeDescription;
            public uint UIFlagDescription;
            public long UIImagePathOffset;
            public long UIVideoPathOffset;
            public float UIPar0;
            public float UIPar1;
            public float UIPar2;
            public float UIPar3;
            public float UIPar4;

            public static RawItem Read(Stream input, Endian endian)
            {
                var instance = new RawItem();
                instance.Name = input.ReadValueU32(endian);
                instance.BaseName = input.ReadValueU32(endian);
                instance.Type = input.ReadValueS32(endian);
                instance.Purchasable = input.ReadValueS32(endian);
                instance.Consumable = input.ReadValueS32(endian);
                instance.Rewardable = input.ReadValueS32(endian);
                instance.Collectable = input.ReadValueS32(endian);
                input.Seek(4, SeekOrigin.Current);
                instance.FeatOffset = input.ReadValueS64(endian);
                instance.FeatCount = input.ReadValueS64(endian);
                instance.Region = input.ReadValueU32(endian);
                instance.RebelDropTimer = input.ReadValueS32(endian);
                instance.MaxInventory = input.ReadValueS32(endian);
                instance.UIType = input.ReadValueS32(endian);
                instance.UIFlag = input.ReadValueS32(endian);
                instance.UIDisplayOrder = input.ReadValueS32(endian);
                instance.UIName = input.ReadValueU32(endian);
                instance.UIDescription = input.ReadValueU32(endian);
                instance.UITypeDescription = input.ReadValueU32(endian);
                instance.UIFlagDescription = input.ReadValueU32(endian);
                instance.UIImagePathOffset = input.ReadValueS64(endian);
                instance.UIVideoPathOffset = input.ReadValueS64(endian);
                instance.UIPar0 = input.ReadValueF32(endian);
                instance.UIPar1 = input.ReadValueF32(endian);
                instance.UIPar2 = input.ReadValueF32(endian);
                instance.UIPar3 = input.ReadValueF32(endian);
                instance.UIPar4 = input.ReadValueF32(endian);
                input.Seek(4, SeekOrigin.Current);
                return instance;
            }

            public static void Write(Stream output, RawItem instance, Endian endian)
            {
                output.WriteValueU32(instance.Name, endian);
                output.WriteValueU32(instance.BaseName, endian);
                output.WriteValueS32(instance.Type, endian);
                output.WriteValueS32(instance.Purchasable, endian);
                output.WriteValueS32(instance.Consumable, endian);
                output.WriteValueS32(instance.Rewardable, endian);
                output.WriteValueS32(instance.Collectable, endian);
                output.Seek(4, SeekOrigin.Current);
                output.WriteValueS64(instance.FeatOffset, endian);
                output.WriteValueS64(instance.FeatCount, endian);
                output.WriteValueU32(instance.Region, endian);
                output.WriteValueS32(instance.RebelDropTimer, endian);
                output.WriteValueS32(instance.MaxInventory, endian);
                output.WriteValueS32(instance.UIType, endian);
                output.WriteValueS32(instance.UIFlag, endian);
                output.WriteValueS32(instance.UIDisplayOrder, endian);
                output.WriteValueU32(instance.UIName, endian);
                output.WriteValueU32(instance.UIDescription, endian);
                output.WriteValueU32(instance.UITypeDescription, endian);
                output.WriteValueU32(instance.UIFlagDescription, endian);
                output.WriteValueS64(instance.UIImagePathOffset, endian);
                output.WriteValueS64(instance.UIVideoPathOffset, endian);
                output.WriteValueF32(instance.UIPar0, endian);
                output.WriteValueF32(instance.UIPar1, endian);
                output.WriteValueF32(instance.UIPar2, endian);
                output.WriteValueF32(instance.UIPar3, endian);
                output.WriteValueF32(instance.UIPar4, endian);
                output.WriteValueU32(0, endian); // TODO(rick): proper pad
            }

            public void Write(Stream output, Endian endian)
            {
                Write(output, this, endian);
            }
        }

        public struct Item
        {
            public uint Name;
            public uint BaseName;
            public int Type;
            public int Purchasable;
            public int Consumable;
            public int Rewardable;
            public int Collectable;
            public uint[] Feats;
            public uint Region;
            public int RebelDropTimer;
            public int MaxInventory;
            public int UIType;
            public int UIFlag;
            public int UIDisplayOrder;
            public uint UIName;
            public uint UIDescription;
            public uint UITypeDescription;
            public uint UIFlagDescription;
            public string UIImagePath;
            public string UIVideoPath;
            public float UIPar0;
            public float UIPar1;
            public float UIPar2;
            public float UIPar3;
            public float UIPar4;
        }
    }
}
