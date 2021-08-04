namespace MCStructureFileEditor
{
    /// <summary>SetBlock方式</summary>
    public enum SetBlockMethod : byte
    {
        /// <summary>替换原位置的方块</summary>
        replace,
        /// <summary>若原位置为空气/未定义，替换为指定的方块</summary>
        keep,
        /// <summary>若原位置未定义，创建指定的方块</summary>
        append
    }

    /// <summary>超出尺寸范围时的处理方式（仅适用于X/Y/Z大于尺寸范围）</summary>
    public enum SetBlockOutOfSizeProcessingMethod : byte
    {
        /// <summary>修改尺寸</summary>
        Resize,
        /// <summary>忽略</summary>
        Ignore,
        /// <summary>SetBlock失败</summary>
        Failed,
        /// <summary>SetBlock异常</summary>
        Exception
    }

    /// <summary>SetPalette超出List范围时的处理方式</summary>
    public enum SetPaletteOutOfSizeProcessingMethod : byte
    {
        /// <summary>扩大调色板</summary>
        Expand,
        /// <summary>SetPalette失败</summary>
        Failed
    }

    /// <summary>方块不存在时的处理方式</summary>
    public enum BlockNotFoundProcessingMethod : byte
    {
        /// <summary>失败</summary>
        Failed,
        /// <summary>异常</summary>
        Exception
    }
}
