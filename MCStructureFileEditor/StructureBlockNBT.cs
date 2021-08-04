using fNbt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MCStructureFileEditor
{
    public class StructureBlockNBT
    {
        /// <summary>
        /// 创建新的结构NBT
        /// </summary>
        /// <param name="nbt">导入的结构NBT</param>
        public StructureBlockNBT(NbtFile nbt)
        {
            Import(in nbt);
        }
        /// <summary>
        /// 创建新的结构NBT
        /// </summary>
        /// <param name="nbt">导入的结构NBT流</param>
        public StructureBlockNBT(Stream nbtStream)
        {
            NbtFile input = new NbtFile(nbtStream);
            Import(in input);
        }
        /// <summary>
        /// 创建新的结构NBT
        /// </summary>
        /// <param name="nbt">导入的结构NBT文件路径</param>
        public StructureBlockNBT(string nbtFile)
        {
            NbtFile input = new NbtFile(nbtFile);
            Import(in input);
        }
        /// <summary>
        /// 创建新的结构NBT
        /// </summary>
        /// <param name="size">结构的大小</param>
        public StructureBlockNBT((int, int, int) size)
        {
            StructureBlockNBT.size = size;
        }
        /// <summary>
        /// 创建新的结构NBT
        /// </summary>
        /// <param name="size">结构的大小</param>
        /// <param name="DataVersion">结构的版本</param>
        public StructureBlockNBT((int, int, int) size, int DataVersion)
        {
            StructureBlockNBT.size = size;
            StructureBlockNBT.DataVersion = DataVersion;
        }
        /// <summary>
        /// 创建新的结构NBT
        /// </summary>
        /// <param name="size">结构的大小</param>
        /// <param name="author">创建该结构的玩家名</param>
        public StructureBlockNBT((int, int, int) size, string author)
        {
            StructureBlockNBT.size = size;
            StructureBlockNBT.author = author;
            haveAuthor = true;
        }
        /// <summary>
        /// 创建新的结构NBT
        /// </summary>
        /// <param name="size">结构的大小</param>
        /// <param name="DataVersion">结构的版本</param>
        /// <param name="author">创建该结构的玩家名</param>
        public StructureBlockNBT((int, int, int) size, int DataVersion, string author)
        {
            StructureBlockNBT.size = size;
            StructureBlockNBT.DataVersion = DataVersion;
            StructureBlockNBT.author = author;
            haveAuthor = true;
        }

        public class BlockInfo
        {
            public int State { get; set; }
            public NbtCompound BlockEntity_NBT { get; set; }
        }
        public class EntityInfo
        {
            public (double, double, double) pos { get; set; }
            public NbtCompound NBT { get; set; }
        }
        public class PaletteItem
        {
            public string Name { get; set; }
            public Dictionary<string, string> Properties { get; set; }

            public override bool Equals(object obj)
            {
                if (obj is null)
                    return false;
                if (GetType() != obj.GetType())
                    return false;

                PaletteItem pi = (PaletteItem)obj;

                if (Name != pi.Name)
                    return false;
                if (Properties is null && pi.Properties is null)
                    return true;
                if (Properties is null && pi.Properties != null)
                    return false;
                if (Properties != null && pi.Properties is null)
                    return false;
                if (Properties.Count != pi.Properties.Count)
                    return false;
                foreach (var kvp in Properties)
                {
                    if (!pi.Properties.TryGetValue(kvp.Key, out string piValue))
                        return false; // key missing in pi
                    if (!Equals(kvp.Value, piValue))
                        return false; // value is different
                }

                return true;
            }

            public override int GetHashCode()
            {
                long x = Name.GetHashCode();
                foreach(var kvp in Properties)
                {
                    x ^= kvp.Key.GetHashCode();
                    x ^= kvp.Value.GetHashCode();
                }
                x <<= 32;
                x >>= 32;
                return Convert.ToInt32(x);
            }
        }


        //NBT结构的版本。
        public static int DataVersion = 0;

        public static bool haveAuthor = false;
        //创建该结构的玩家名，只存在于1.13前保存的结构。
        public static string author = "";

        //3 int 用于描述结构的大小。
        public static (int, int, int) size = (0, 0, 0);

        //结构中各个方块的字典。
        public static Dictionary<(int, int, int), BlockInfo> blocks = new Dictionary<(int, int, int), BlockInfo>();

        //结构中的实体列表。
        public static List<EntityInfo> entities = new List<EntityInfo>();

        public static bool multiplePalette = false;
        //在结构中使用的不同方块状态的集合。
        public static List<PaletteItem> palette = new List<PaletteItem>();
        //在结构中使用的不同方块状态的集合，基于坐标和结构完整性来选择随机调色板。在原版中用于沉船。
        public static List<List<PaletteItem>> palettes = new List<List<PaletteItem>>();


        #region StructureNBT-Clear
        /// <summary>
        /// 清除结构NBT
        /// </summary>
        /// <returns>操作成功返回true</returns>
        public bool Clear()
        {
            haveAuthor = false;
            multiplePalette = false;
            DataVersion = 0;
            author = "";
            size = (0, 0, 0);
            blocks = new Dictionary<(int, int, int), BlockInfo>();
            entities = new List<EntityInfo>();
            palette = new List<PaletteItem>();
            palettes = new List<List<PaletteItem>>();
            return true;
        }
        #endregion StructureNBT-Clear

        #region StructureNBT-Import
        /// <summary>
        /// 导入结构NBT
        /// </summary>
        /// <param name="nbt_input">导入的结构NBT</param>
        /// <returns>操作成功返回true</returns>
        public bool Import(in NbtFile nbt_input)
        {
            byte importStatus = 0x00;
            bool haventReadPalette = true;

            foreach (NbtTag nt in nbt_input.RootTag.Tags)
            {
                //////////////////////////////////////////////////////////////////////////////////////// 版本
                if (nt.TagType == NbtTagType.Int && nt.Name == "DataVersion")
                {
                    DataVersion = nt.IntValue;
                    importStatus = (byte)(importStatus | 0x01);
                }
                //////////////////////////////////////////////////////////////////////////////////////// 作者，1.13前
                else if (nt.TagType == NbtTagType.String && nt.Name == "author")
                {
                    author = nt.StringValue;
                    importStatus = (byte)(importStatus | 0x02);
                    haveAuthor = true;
                }
                //////////////////////////////////////////////////////////////////////////////////////// 尺寸
                else if (nt.TagType == NbtTagType.List && nt.Name == "size")
                {
                    NbtList temp = ((NbtList)nt);
                    if (temp.ListType == NbtTagType.Int)
                    {
                        NbtList nl = (NbtList)nt;
                        size.Item1 = nl[0].IntValue;
                        size.Item2 = nl[1].IntValue;
                        size.Item3 = nl[2].IntValue;
                        importStatus = (byte)(importStatus | 0x04);
                    }
                }
                //////////////////////////////////////////////////////////////////////////////////////// 单调色板
                else if (haventReadPalette && nt.TagType == NbtTagType.List && nt.Name == "palette")
                {
                    NbtList temp = (NbtList)nt;
                    if (temp.ListType == NbtTagType.Compound)
                    {
                        foreach (NbtCompound nc in (NbtList)nt)
                        {
                            NbtCompound properties_nc = nc.Get<NbtCompound>("Properties");
                            if (properties_nc != null)
                            {
                                Dictionary<string, string> properties_dic = new Dictionary<string, string>();
                                foreach (NbtString item in properties_nc)
                                {
                                    properties_dic.Add(item.Name, item.StringValue);
                                }
                                palette.Add(new PaletteItem() { Name = nc.Get<NbtString>("Name").StringValue, Properties = properties_dic });
                            }
                            else
                            {
                                palette.Add(new PaletteItem() { Name = nc.Get<NbtString>("Name").StringValue, Properties = null });
                            }
                        }
                        importStatus = (byte)(importStatus | 0x08);
                        multiplePalette = false;
                        haventReadPalette = false;
                    }
                }
                //////////////////////////////////////////////////////////////////////////////////////// 多调色板
                else if (haventReadPalette && nt.TagType == NbtTagType.List && nt.Name == "palettes")
                {
                    NbtList temp = (NbtList)nt;
                    if (temp.ListType == NbtTagType.List)
                    {
                        foreach (NbtList nl in (NbtList)nt)
                        {
                            List<PaletteItem> lpi = new List<PaletteItem>();
                            foreach (NbtCompound nc in (NbtList)nt)
                            {
                                NbtCompound properties_nc = nc.Get<NbtCompound>("Properties");
                                if (properties_nc != null)
                                {
                                    Dictionary<string, string> properties_dic = new Dictionary<string, string>();
                                    foreach (NbtString item in properties_nc)
                                    {
                                        properties_dic.Add(item.Name, item.StringValue);
                                    }
                                    lpi.Add(new PaletteItem() { Name = nc.Get<NbtString>("Name").StringValue, Properties = properties_dic });
                                }
                                else
                                {
                                    lpi.Add(new PaletteItem() { Name = nc.Get<NbtString>("Name").StringValue, Properties = null });
                                }
                            }
                            palettes.Add(lpi);
                            importStatus = (byte)(importStatus | 0x10);
                            multiplePalette = true;
                            haventReadPalette = false;
                        }
                    }
                }
                //////////////////////////////////////////////////////////////////////////////////////// 方块
                else if (nt.TagType == NbtTagType.List && nt.Name == "blocks")
                {
                    NbtList temp = (NbtList)nt;
                    if (temp.ListType == NbtTagType.Compound)
                    {
                        foreach (NbtCompound nc in (NbtList)nt)
                        {
                            NbtList nl = nc.Get<NbtList>("pos");
                            (int, int, int) pos = (nl.Get<NbtInt>(0).IntValue, nl.Get<NbtInt>(1).IntValue, nl.Get<NbtInt>(2).IntValue);
                            blocks[pos] = new BlockInfo() { State = nc.Get<NbtInt>("state").IntValue, BlockEntity_NBT = nc.Get<NbtCompound>("nbt") };
                        }
                        importStatus = (byte)(importStatus | 0x20);
                    }
                }
                //////////////////////////////////////////////////////////////////////////////////////// 实体
                else if (nt.TagType == NbtTagType.List && nt.Name == "entities")
                {
                    NbtList temp = (NbtList)nt;
                    if (temp.ListType == NbtTagType.Compound)
                    {
                        foreach (NbtCompound nc in (NbtList)nt)
                        {
                            NbtList nl = nc.Get<NbtList>("pos");
                            entities.Add(new EntityInfo()
                            {
                                pos = (nl.Get<NbtDouble>(0).DoubleValue, nl.Get<NbtDouble>(1).DoubleValue, nl.Get<NbtDouble>(2).DoubleValue),
                                NBT = nc.Get<NbtCompound>("nbt")
                            });
                        }
                        importStatus = (byte)(importStatus | 0x40);
                    }
                }
            }

            if (!haveAuthor) haveAuthor = false;

            if (importStatus == 0x6D || importStatus == 0x6F || importStatus == 0x75 || importStatus == 0x77)
            {
                return true;
            }
            else
            {
                throw new NbtFormatException("输入的结构方块NBT不正确");
            }

        }
        #endregion StructureNBT-Import

        #region StructureNBT-Export
        /// <summary>
        /// 导出结构NBT
        /// </summary>
        /// <param name="nbt_output">导出的结构NBT</param>
        /// <returns>操作成功返回true</returns>
        public bool Export(out NbtFile nbt_output)
        {
            if (size.Item1 > 48) size.Item1 = 48;
            if (size.Item2 > 48) size.Item2 = 48;
            if (size.Item3 > 48) size.Item3 = 48;

            NbtCompound output_roottag = new NbtCompound("")
            {
                new NbtInt("DataVersion", DataVersion),
                new NbtList("size") {
                    new NbtInt(size.Item1),
                    new NbtInt(size.Item2),
                    new NbtInt(size.Item3)
                },
                new NbtList("blocks",NbtTagType.Compound),
                new NbtList("entities",NbtTagType.Compound)
            };

            if (haveAuthor)
            {
                output_roottag.Add(new NbtString("author", author));
            }

            //////////////////////////////////////////////////////////////////////////////////////// 调色板
            if (multiplePalette)
            {
                output_roottag.Add(new NbtList("palettes", NbtTagType.List));
                foreach (List<PaletteItem> paletteList in palettes)
                {
                    NbtList palette_x = new NbtList(NbtTagType.Compound);
                    foreach (PaletteItem paletteItem in paletteList)
                    {
                        NbtCompound temp = new NbtCompound();
                        if (paletteItem is null)
                        {
                            temp.Add(new NbtString("Name", ""));
                        }
                        else
                        {
                            temp.Add(new NbtString("Name", paletteItem.Name));
                            if (paletteItem.Properties != null)
                            {
                                NbtCompound properties = new NbtCompound("Properties");
                                foreach (KeyValuePair<string, string> propertiesItem in paletteItem.Properties)
                                {
                                    properties.Add(new NbtString(propertiesItem.Key, propertiesItem.Value));
                                }
                                temp.Add(new NbtCompound(properties));
                            }
                        }
                        palette_x.Add(temp);
                    }
                    output_roottag.Get<NbtList>("palettes").Add(palette_x);
                }
            }
            else
            {
                output_roottag.Add(new NbtList("palette", NbtTagType.Compound));
                foreach (PaletteItem paletteItem in palette)
                {
                    NbtCompound temp = new NbtCompound();
                    if (paletteItem is null)
                    {
                        temp.Add(new NbtString("Name", ""));
                    }
                    else
                    {
                        temp.Add(new NbtString("Name", paletteItem.Name));
                        if (paletteItem.Properties != null)
                        {
                            NbtCompound properties = new NbtCompound("Properties");
                            foreach (KeyValuePair<string, string> propertiesItem in paletteItem.Properties)
                            {
                                properties.Add(new NbtString(propertiesItem.Key, propertiesItem.Value));
                            }
                            temp.Add(new NbtCompound(properties));
                        }
                    }
                    output_roottag.Get<NbtList>("palette").Add(temp);

                }
            }
            //////////////////////////////////////////////////////////////////////////////////////// 方块
            foreach (KeyValuePair<(int, int, int), BlockInfo> block in blocks)
            {
                NbtCompound block_tag = new NbtCompound() {
                    new NbtInt("state",block.Value.State),
                    new NbtList("pos")
                    {
                        new NbtInt(block.Key.Item1),
                        new NbtInt(block.Key.Item2),
                        new NbtInt(block.Key.Item3)
                    },
                };
                if (block.Value.BlockEntity_NBT != null)
                {
                    block_tag.Add(new NbtCompound(block.Value.BlockEntity_NBT));
                }
                output_roottag.Get<NbtList>("blocks").Add(block_tag);

            }
            //////////////////////////////////////////////////////////////////////////////////////// 实体
            foreach (EntityInfo entity in entities)
            {
                if (entity.NBT is null)
                {
                    throw new ArgumentNullException("实体NBT不能为空");
                }
                output_roottag.Get<NbtList>("entities").Add(new NbtCompound() {
                    new NbtList("pos")
                    {
                        new NbtDouble(entity.pos.Item1),
                        new NbtDouble(entity.pos.Item2),
                        new NbtDouble(entity.pos.Item3)
                    },
                    new NbtList("blockPos")
                    {
                        new NbtInt((int)Math.Floor(entity.pos.Item1)),
                        new NbtInt((int)Math.Floor(entity.pos.Item2)),
                        new NbtInt((int)Math.Floor(entity.pos.Item3))
                    },
                    new NbtCompound(entity.NBT)
                });
            }

            nbt_output = new NbtFile(output_roottag);
            return true;
        }
        #endregion StructureNBT-Export

        #region StructureNBT-SetBlock
        /// <summary>
        /// 将一个方块更改为另一个方块。
        /// </summary>
        /// <param name="pos">坐标 (x, y, z)</param>
        /// <param name="blockName">方块名</param>
        /// <param name="method">原方块处理方式</param>
        /// <param name="outOfSizeProcess">超出尺寸范围时的处理方式（仅适用于X/Y/Z大于尺寸范围）</param>
        /// <param name="blockStateProperties">方块状态属性</param>
        /// <param name="blockEntity_nbt">方块实体NBT</param>
        /// <returns>操作成功则返回true，否则返回false</returns>
        public bool SetBlock((int, int, int) pos, string blockName, SetBlockMethod method = SetBlockMethod.replace,
                            SetBlockOutOfSizeHandle outOfSizeProcess = SetBlockOutOfSizeHandle.Resize,
                            Dictionary<string, string> blockStateProperties = null, NbtCompound blockEntity_nbt = null)
        {
            switch (outOfSizeProcess)
            {
                case SetBlockOutOfSizeHandle.Resize:
                    if (pos.Item1 >= size.Item1) size.Item1 = pos.Item1 + 1;
                    if (pos.Item2 >= size.Item2) size.Item2 = pos.Item2 + 1;
                    if (pos.Item3 >= size.Item3) size.Item3 = pos.Item3 + 1;
                    break;
                case SetBlockOutOfSizeHandle.Failed:
                    if (pos.Item1 >= size.Item1 || pos.Item1 < 0) return false;
                    if (pos.Item2 >= size.Item2 || pos.Item2 < 0) return false;
                    if (pos.Item3 >= size.Item3 || pos.Item3 < 0) return false;
                    break;
                case SetBlockOutOfSizeHandle.Exception:
                    if (pos.Item1 >= size.Item1 || pos.Item1 < 0) throw new ArgumentOutOfRangeException("X坐标超出尺寸范围");
                    if (pos.Item2 >= size.Item2 || pos.Item2 < 0) throw new ArgumentOutOfRangeException("Y坐标超出尺寸范围");
                    if (pos.Item3 >= size.Item3 || pos.Item3 < 0) throw new ArgumentOutOfRangeException("Z坐标超出尺寸范围");
                    break;
            }
            int state = 0;
            if (!blockName.Contains(":"))
            {
                blockName = "minecraft:" + blockName;
            }
            PaletteItem newpalette = new PaletteItem()
            { Name = blockName, Properties = blockStateProperties };
            switch (method)
            {
                //////////////////////////////////////////////////////////////////////////////////////// SetBlockMethod.replace
                case SetBlockMethod.replace:
                    if (multiplePalette)
                    {
                        foreach (List<PaletteItem> item in palettes)
                        {
                            item.Add(newpalette);
                            state = item.Count - 1;
                        }
                    }
                    else
                    {
                        state = palette.FindIndex(x => x.Equals(newpalette));
                        if (state == -1)
                        {
                            palette.Add(newpalette);
                            state = palette.Count - 1;
                        }
                    }
                    blocks[pos] = new BlockInfo { State = state, BlockEntity_NBT = blockEntity_nbt };
                    return true;
                //////////////////////////////////////////////////////////////////////////////////////// SetBlockMethod.keep
                case SetBlockMethod.keep:
                    bool next = false;
                    if (blocks.TryGetValue(pos, out BlockInfo bi))
                    {
                        if (multiplePalette)
                        {
                            foreach (List<PaletteItem> lpi in palettes)
                            {
                                if (palette[bi.State].Name == "minecraft:air" ||
                                   palette[bi.State].Name == "minecraft:cave_air" ||
                                   palette[bi.State].Name == "minecraft:void_air" ||
                                   palette[bi.State].Name == "air")
                                {
                                    next = true;
                                }
                            }
                        }
                        else
                        {
                            if (palette[bi.State].Name == "minecraft:air" ||
                                palette[bi.State].Name == "minecraft:cave_air" ||
                                palette[bi.State].Name == "minecraft:void_air" ||
                                palette[bi.State].Name == "air")
                            {
                                next = true;
                            }

                        }
                    }
                    else
                    {
                        next = true;
                    }

                    if (next)
                    {
                        if (palette.Count > 0 && palettes.Count > 0)
                        {
                            if (multiplePalette)
                            {
                                foreach (List<PaletteItem> item in palettes)
                                {
                                    item.Add(newpalette);
                                }
                                state = palettes[0].Count - 1;
                            }
                            else
                            {
                                state = palette.FindIndex(x => x.Equals(newpalette));
                                if (state == -1)
                                {
                                    palette.Add(newpalette);
                                    state = palette.Count - 1;
                                }
                            }
                        }
                        else
                        {
                            if (multiplePalette)
                            {
                                palettes.Add(new List<PaletteItem>() { newpalette });
                                state = palettes[0].Count - 1;
                            }
                            else
                            {
                                palette.Add(newpalette);
                                state = palette.Count - 1;
                            }
                        }
                        blocks[pos] = new BlockInfo { State = state, BlockEntity_NBT = blockEntity_nbt };
                        return true;
                    }
                    throw new Exception("目标位置方块已存在且不为空气");
                //////////////////////////////////////////////////////////////////////////////////////// SetBlockMethod.append
                case SetBlockMethod.append:
                    if (!blocks.TryGetValue(pos, out BlockInfo bi2))
                    {
                        if (palette.Count > 0 && palettes.Count > 0)
                        {
                            if (multiplePalette)
                            {
                                foreach (List<PaletteItem> item in palettes)
                                {
                                    item.Add(newpalette);
                                }
                                state = palettes[0].Count - 1;
                            }
                            else
                            {
                                state = palette.FindIndex(x => x.Equals(newpalette));
                                if (state == -1)
                                {
                                    palette.Add(newpalette);
                                    state = palette.Count - 1;
                                }
                            }
                        }
                        else
                        {
                            if (multiplePalette)
                            {
                                palettes.Add(new List<PaletteItem>() { newpalette });
                                state = palettes[0].Count - 1;
                            }
                            else
                            {
                                palette.Add(newpalette);
                                state = palette.Count - 1;
                            }
                        }
                        blocks[pos] = new BlockInfo { State = state, BlockEntity_NBT = blockEntity_nbt };
                        return true;
                    }
                    throw new Exception("目标位置方块已存在");

            }
            throw new Exception();
        }

        /// <summary>
        /// 将一个方块更改为另一个方块。
        /// </summary>
        /// <param name="pos">坐标 (x, y, z)</param>
        /// <param name="state">调色板中方块的索引</param>
        /// <param name="method">原方块处理方式</param>
        /// <param name="outOfSizeProcess">超出尺寸范围时的处理方式（仅适用于X/Y/Z大于尺寸范围）</param>
        /// <param name="blockEntity_nbt">方块实体NBT</param>
        /// <returns>操作成功则返回true，否则返回false</returns>
        public bool SetBlock((int, int, int) pos, int state, SetBlockMethod method = SetBlockMethod.replace,
                             SetBlockOutOfSizeHandle outOfSizeProcess = SetBlockOutOfSizeHandle.Resize, NbtCompound blockEntity_nbt = null)
        {
            switch (outOfSizeProcess)
            {
                case SetBlockOutOfSizeHandle.Resize:
                    if (pos.Item1 >= size.Item1) size.Item1 = pos.Item1 + 1;
                    if (pos.Item2 >= size.Item2) size.Item2 = pos.Item2 + 1;
                    if (pos.Item3 >= size.Item3) size.Item3 = pos.Item3 + 1;
                    break;
                case SetBlockOutOfSizeHandle.Failed:
                    if (pos.Item1 >= size.Item1 || pos.Item1 < 0) return false;
                    if (pos.Item2 >= size.Item2 || pos.Item2 < 0) return false;
                    if (pos.Item3 >= size.Item3 || pos.Item3 < 0) return false;
                    break;
                case SetBlockOutOfSizeHandle.Exception:
                    if (pos.Item1 >= size.Item1 || pos.Item1 < 0) throw new ArgumentOutOfRangeException("X坐标超出尺寸范围");
                    if (pos.Item2 >= size.Item2 || pos.Item2 < 0) throw new ArgumentOutOfRangeException("Y坐标超出尺寸范围");
                    if (pos.Item3 >= size.Item3 || pos.Item3 < 0) throw new ArgumentOutOfRangeException("Z坐标超出尺寸范围");
                    break;
            }
            switch (method)
            {
                //////////////////////////////////////////////////////////////////////////////////////// SetBlockMethod.replace
                case SetBlockMethod.replace:
                    blocks[pos] = new BlockInfo { State = state, BlockEntity_NBT = blockEntity_nbt };
                    return true;
                //////////////////////////////////////////////////////////////////////////////////////// SetBlockMethod.keep
                case SetBlockMethod.keep:
                    bool next = false;
                    if (blocks.TryGetValue(pos, out BlockInfo bi))
                    {
                        if (multiplePalette)
                        {
                            foreach (List<PaletteItem> lpi in palettes)
                            {
                                if (palette[bi.State].Name == "minecraft:air" ||
                                   palette[bi.State].Name == "minecraft:cave_air" ||
                                   palette[bi.State].Name == "minecraft:void_air" ||
                                   palette[bi.State].Name == "air")
                                {
                                    next = true;
                                }
                            }
                        }
                        else
                        {
                            if (palette[bi.State].Name == "minecraft:air" ||
                                palette[bi.State].Name == "minecraft:cave_air" ||
                                palette[bi.State].Name == "minecraft:void_air" ||
                                palette[bi.State].Name == "air")
                            {
                                next = true;
                            }

                        }
                    }
                    else
                    {
                        next = true;
                    }

                    if (next)
                    {
                        blocks[pos] = new BlockInfo { State = state, BlockEntity_NBT = blockEntity_nbt };
                        return true;
                    }
                    throw new Exception("目标位置方块已存在且不为空气");
                //////////////////////////////////////////////////////////////////////////////////////// SetBlockMethod.append
                case SetBlockMethod.append:
                    if (!blocks.TryGetValue(pos, out _))
                    {
                        blocks[pos] = new BlockInfo { State = state, BlockEntity_NBT = blockEntity_nbt };
                        return true;
                    }
                    throw new Exception("目标位置方块已存在");
            }
            throw new Exception();
        }
        #endregion StructureNBT-SetBlock

        #region StructureNBT-RemoveBlock

        #endregion StructureNBT-RemoveBlock

        #region StructureNBT-PaletteSetting
        /// <summary>
        /// 设置调色板
        /// </summary>
        /// <param name="newPalette">新调色板</param>
        /// <returns>操作成功则返回true</returns>
        public bool SetPalette(List<PaletteItem> newPalette)
        {
            palette = newPalette;
            palettes = new List<List<PaletteItem>>();
            multiplePalette = false;
            return true;
        }
        /// <summary>
        /// 设置多调色板
        /// </summary>
        /// <param name="newPalette">新调色板</param>
        /// <returns>操作成功则返回true</returns>
        public bool SetPalette(List<List<PaletteItem>> newPalettes)
        {
            palette = new List<PaletteItem>();
            palettes = newPalettes;
            multiplePalette = true;
            return true;
        }

        /// <summary>
        /// 设置调色板
        /// </summary>
        /// <param name="index">调色板项索引</param>
        /// <param name="newPaletteItem">新调色板项</param>
        /// <param name="outOfSizeProcess">SetPalette超出List范围时的处理方式</param>
        /// <returns>操作成功则返回true</returns>
        public bool SetPalette(int index, PaletteItem newPaletteItem, SetPaletteOutOfSizeHandle outOfSizeProcess = SetPaletteOutOfSizeHandle.Expand)
        {
            if (multiplePalette)
            {
                for (int i = 0; i < palettes.Count; i++)
                {
                    if (index <= palettes[i].Count)
                        palettes[i][index] = newPaletteItem;
                    else if (outOfSizeProcess == SetPaletteOutOfSizeHandle.Expand)
                    {
                        PaletteItem[] tmp_array = new PaletteItem[index + 1];
                        palettes[i].CopyTo(tmp_array);
                        tmp_array[index] = newPaletteItem;
                        palettes[i] = tmp_array.ToList();
                    }
                    else throw new IndexOutOfRangeException();
                }
            }
            else
            {
                if (index <= palette.Count)
                    palette[index] = newPaletteItem;
                else if (outOfSizeProcess == SetPaletteOutOfSizeHandle.Expand)
                {
                    PaletteItem[] tmp_array = new PaletteItem[index + 1];
                    palette.CopyTo(tmp_array);
                    tmp_array[index] = newPaletteItem;
                    palette = tmp_array.ToList();
                }
                else throw new IndexOutOfRangeException();
            }
            return true;
        }
        /// <summary>
        /// 设置多调色板
        /// </summary>
        /// <param name="index">调色板索引</param>
        /// <param name="newPalette">新调色板</param>
        /// <param name="outOfSizeProcess">SetPalette超出List范围时的处理方式</param>
        /// <returns>操作成功则返回true</returns>
        public bool SetPalette(int index, List<PaletteItem> newPalette, SetPaletteOutOfSizeHandle outOfSizeProcess = SetPaletteOutOfSizeHandle.Expand)
        {
            if (!multiplePalette) throw new NotSupportedException();
            if (index <= palettes.Count)
                palettes[index] = newPalette;
            else if (outOfSizeProcess == SetPaletteOutOfSizeHandle.Expand)
            {
                List<PaletteItem>[] tmp_array = new List<PaletteItem>[index + 1];
                palettes.CopyTo(tmp_array);
                tmp_array[index] = newPalette;
                palettes = tmp_array.ToList();
            }
            else throw new IndexOutOfRangeException();
            return true;
        }
        /// <summary>
        /// 设置多调色板项
        /// </summary>
        /// <param name="paletteIndex"></param>
        /// <param name="paletteItemIndex"></param>
        /// <param name="newPalette"></param>
        /// <param name="outOfSizeProcess"></param>
        /// <returns>操作成功则返回true</returns>
        public bool SetPalette(int paletteIndex, int paletteItemIndex, PaletteItem newPalette, SetPaletteOutOfSizeHandle outOfSizeProcess = SetPaletteOutOfSizeHandle.Expand)
        {
            if (!multiplePalette) throw new NotSupportedException();
            if (paletteIndex > palettes.Count)
            {
                if (outOfSizeProcess != SetPaletteOutOfSizeHandle.Expand) throw new IndexOutOfRangeException();
                List<PaletteItem>[] tmp_array = new List<PaletteItem>[paletteIndex + 1];
                palettes.CopyTo(tmp_array);
                palettes = tmp_array.ToList();
            }
            if (paletteItemIndex > palettes[paletteIndex].Count)
            {
                if (outOfSizeProcess != SetPaletteOutOfSizeHandle.Expand) throw new IndexOutOfRangeException();
                PaletteItem[] tmp_array = new PaletteItem[paletteItemIndex + 1];
                palettes[paletteItemIndex].CopyTo(tmp_array);
                palettes[paletteItemIndex] = tmp_array.ToList();
            }
            palettes[paletteIndex][paletteItemIndex] = newPalette;
            return true;
        }

        /// <summary>
        /// 添加调色板项
        /// </summary>
        /// <param name="newPaletteItem">新调色板项</param>
        /// <returns>操作成功则返回true</returns>
        public bool AddPalette(PaletteItem newPaletteItem)
        {
            if (multiplePalette) throw new NotSupportedException();
            palette.Add(newPaletteItem);
            return true;
        }
        /// <summary>
        /// 添加调色板
        /// </summary>
        /// <param name="newPalette">新调色板</param>
        /// <returns>操作成功则返回true</returns>
        public bool AddPalette(List<PaletteItem> newPalette)
        {
            if (!multiplePalette) throw new NotSupportedException();
            palettes.Add(newPalette);
            return true;
        }
        #endregion StructureNBT-PaletteSetting
    }
}