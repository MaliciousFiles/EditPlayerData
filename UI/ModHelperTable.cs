using System;
using System.Collections.Generic;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Extensions;
using Il2CppNinjaKiwi.Common;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace EditPlayerData.UI;

[RegisterTypeInIl2Cpp(false)]
public class ModHelperTable : ModHelperPanel
{
    private readonly List<ModHelperTableRow> _rows = new();
    private ModHelperScrollPanel? _content;
    private float _rowSpacing;
    private float _rowWidth;
    private float _rowHeight;
    private int _columns;
    private int[] _colFlex = Array.Empty<int>();

    public int RowCount => _rows.Count;

    public ModHelperTable(IntPtr ptr) : base(ptr) { }

    public static ModHelperTable Create(Info info, int columnCount, string? backgroundSprite = null, float colSpacing = 0.0f,
        int[]? colFlex = null, float rowSpacing = 0.0f, float rowHeight = 0.0f, RectOffset? padding = null)
    {
        var table = Create<ModHelperTable>(info, backgroundSprite, RectTransform.Axis.Vertical,
            colSpacing);
        if (padding != null) table.LayoutGroup.padding = padding;
        table._rowSpacing = rowSpacing;
        table._rowWidth = rowHeight == 0 ? 0 : table.RectTransform.rect.width - (padding?.horizontal ?? 0);
        table._rowHeight = rowHeight;
        table._columns = columnCount;

        if (colFlex != null) table._colFlex = colFlex;

        table._content = table.AddScrollPanel(new Info("Content", InfoPreset.Flex),
            RectTransform.Axis.Vertical, null, rowSpacing);
        table._content.AddComponent<Image>().color = Color.black;
        table._content.Mask.showMaskGraphic = false;
        
        // don't know if this always happens, but at least in the MapPlayerDataSetting usecase, it has a weird visible section above the main
        // part, so this just hides anything that shouldn't exist
        table._content.ScrollRect.onValueChanged.AddListener(new Action<Vector2>(_ =>
        {
            // for some reason top is negative and bottom is positive, so these are subtraction to get the expected values
            var viewportTransform = table._content.ScrollRect.viewport;
            var viewportPos = viewportTransform.position;
            var viewportRect = viewportTransform.rect;
            var viewportTop = viewportPos.y - viewportRect.top;
            var viewportBottom = viewportPos.y - viewportRect.bottom;

            var scrollContent = table._content.ScrollContent.transform;
            for (var c = 0; c < scrollContent.childCount; c++)
            {
                var child = scrollContent.GetChild(c);

                var inputTransform = child.gameObject.GetComponent<RectTransform>();
                var inputPos = inputTransform!.position;
                var inputRect = inputTransform.rect;
                var inputTop = inputPos.y - inputRect.top;
                var inputBottom = inputPos.y - inputRect.bottom;

                for (var i = 0; i < child.childCount; i++)
                {
                    child.GetChild(i).gameObject.SetActive(
                        (inputBottom < viewportTop && inputBottom > viewportBottom) ||
                        (inputTop < viewportTop && inputTop > viewportBottom));
                }
            }
        }));


        return table;
    }
    
    public void AddRow()
    {
        var row = ModHelperTableRow.Create(RowCount, new Info($"Row{_rows.Count+1}")
            {
                Height = _rowHeight, Width = _rowWidth,
                Flex = _rowHeight == 0 ? 1 : 0
            }, null, RectTransform.Axis.Horizontal, _rowSpacing);
        _content!.AddScrollContent(row);
        
        for (var i = 0; i < _columns; i++)
        {
            row.Add(Create(new Info($"Col{i + 1}", InfoPreset.Flex)
            {
                FlexWidth = i > _colFlex.Length ? 1 : _colFlex[i]
            }));
        }
        
        _rows.Add(row);
    }

    public void SetValue(int r, int c, ModHelperComponent value)
    {
        // due to how masking works with input fields, we need to "reload" them
        if (value.GetType() == typeof(ModHelperInputField))
        {
            value.SetActive(false);
            value.SetActive(true);

        }

        if (c > _columns) SetNumColumns(c);
        for (var i = _rows.Count-1; i < r; i++) AddRow();
        
        // if no other size is given, just fill the whole cell
        var info = value.initialInfo;
        if (info.SizeDelta == Vector2.zero && info.AnchorMin == new Vector2(0.5f, 0.5f) &&
            info.AnchorMax == new Vector2(0.5f, 0.5f))
        {
            value.SetInfo(new Info(info.Name, InfoPreset.FillParent) { X = info.X, Y = info.Y });
        }
        
        var col = _rows[r].GetDescendent<ModHelperPanel>($"Col{c + 1}");
        col.transform.DestroyAllChildren();
        col.Add(value);
    }

    public void RemoveRow(int index)
    {
        _rows[index].Destroy();
        _rows.RemoveAt(index);
    }

    public ModHelperTableRow GetRow(int index)
    {
        return _rows[index];
    }

    public void SetNumColumns(int numCols, int[]? colFlex = null)
    {
        if (colFlex != null) _colFlex = colFlex;
        _columns = numCols;

        foreach (var row in _rows)
        {
            var numChildren = row.transform.GetChildCount();

            if (numChildren < numCols)
            {
                for (var i = 0; i < numChildren - numCols; i++)
                {
                    row.AddPanel(new Info($"Col{numChildren + i + 1}", InfoPreset.Flex)
                    {
                        FlexWidth = i > _colFlex.Length ? 1 : _colFlex[i]
                    });
                }
            } else if (numCols < numChildren)
            {
                for (var i = numChildren; i >= numCols; i++)
                {
                    row.transform.GetChild(i).Destroy();
                }
            }
        }
    }

    public void ScrollTo(int rowIdx)
    {
        var scrollHeight = _content!.ScrollContent.RectTransform.rect.height;
        var viewportHeight = _content.ScrollRect.viewport.rect.height;
        
        _content.ScrollRect.SetContentAnchoredPosition(new Vector2(0, Math.Clamp(
            Math.Abs(_rows[rowIdx].transform.localPosition.y),
            viewportHeight/2, scrollHeight - viewportHeight/2)));
    }
}