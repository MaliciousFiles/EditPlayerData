using System;
using BTD_Mod_Helper.Api.Components;
using MelonLoader;
using UnityEngine;

namespace EditPlayerData.UI;

[RegisterTypeInIl2Cpp(false)]
public class ModHelperTableRow : ModHelperPanel
{
    public int Row { get; private set; }

    public ModHelperTableRow(IntPtr ptr) : base(ptr) { }
    
    public static ModHelperTableRow Create(
        int rowIdx,
        Info info,
        string? backgroundSprite = null,
        RectTransform.Axis? layoutAxis = null,
        float spacing = 0.0f,
        int padding = 0)
    {
        var row = Create<ModHelperTableRow>(info, backgroundSprite, layoutAxis, spacing, padding);
        row.Row = rowIdx;

        return row;
    }
}