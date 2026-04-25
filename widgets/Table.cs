using System.Reflection;
using System.Text.Json;
using SkiaSharp;

namespace SkiaUiLibrary.Widgets;

public class Table
{
    private const float RowH    = 67f;
    private const float RowsTop = 124f;
    private const float LeftX   = 40f;
    private const float DotsW   = 52f;
    private const float CardM   = 12f;

    private readonly string     _title;
    private readonly string[]   _columnKeys;
    private readonly string[]   _columnHeaders;
    private readonly string[][] _rows;

    // Palette
    private static readonly SKColor CBg      = new(0x13, 0x15, 0x1A);
    private static readonly SKColor CCard    = new(0x1C, 0x1F, 0x27);
    private static readonly SKColor CBorder  = new(0x2C, 0x2F, 0x3C);
    private static readonly SKColor CPrim    = new(0xE2, 0xE8, 0xF0);
    private static readonly SKColor CMuted   = new(0x6B, 0x72, 0x80);
    private static readonly SKColor CDivider = new(0x23, 0x26, 0x30);

    private static readonly Dictionary<string, (SKColor bg, SKColor border, SKColor text)> BadgeColors
        = new(StringComparer.OrdinalIgnoreCase)
        {
            ["completed"]  = (new(0x0F, 0x2E, 0x1A), new(0x1A, 0x6B, 0x35), new(0x4A, 0xDE, 0x80)),
            ["processing"] = (new(0x2D, 0x1F, 0x05), new(0xB9, 0x7D, 0x10), new(0xF5, 0x9E, 0x0B)),
            ["failed"]     = (new(0x2D, 0x0A, 0x0A), new(0x8B, 0x20, 0x20), new(0xEF, 0x44, 0x44)),
        };

    private static readonly (SKColor bg, SKColor border, SKColor text) BadgeFallback =
        (new(0x1E, 0x22, 0x2A), new(0x3A, 0x3F, 0x50), new(0xA0, 0xAA, 0xB8));

    /// <summary>Preferred client height to fit all rows without clipping.</summary>
    public int PreferredHeight => (int)(RowsTop + _rows.Length * RowH + 36);

    // Shared constructor — all public entry points funnel here.
    private Table(string title, string[] columnKeys, string[] columnHeaders, string[][] rows)
    {
        _title         = title;
        _columnKeys    = columnKeys;
        _columnHeaders = columnHeaders;
        _rows          = rows;
    }

    /// <summary>Build a table from a JSON array string.</summary>
    public Table(string title, string json)
        : this(title, ParseJson(json)) { }

    /// <summary>Build a table from any IEnumerable of a C# class/record using reflection.</summary>
    public static Table From<T>(string title, IEnumerable<T> items) where T : notnull
    {
        var props   = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var keys    = props.Select(p => PascalToSnakeCase(p.Name)).ToArray();
        var headers = keys.Select(FormatHeader).ToArray();
        var rows    = items.Select(item =>
            props.Select(p => p.GetValue(item)?.ToString() ?? "").ToArray()
        ).ToArray();

        return new Table(title, keys, headers, rows);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static (string[] keys, string[] headers, string[][] rows) ParseJson(string json)
    {
        var docs = JsonSerializer.Deserialize<JsonElement[]>(json)
            ?? throw new ArgumentException("Invalid JSON array.", nameof(json));
        if (docs.Length == 0)
            throw new ArgumentException("JSON array must not be empty.", nameof(json));

        var keys    = docs[0].EnumerateObject().Select(p => p.Name).ToArray();
        var headers = keys.Select(FormatHeader).ToArray();
        var rows    = docs.Select(doc =>
            keys.Select(k => doc.TryGetProperty(k, out var v) ? v.GetString() ?? "" : "").ToArray()
        ).ToArray();

        return (keys, headers, rows);
    }

    private Table(string title, (string[] keys, string[] headers, string[][] rows) d)
        : this(title, d.keys, d.headers, d.rows) { }

    // "transaction_id" → "Transaction Id"
    private static string FormatHeader(string key) =>
        string.Join(" ", key.Split('_')
            .Select(w => w.Length > 0 ? char.ToUpper(w[0]) + w[1..] : w));

    // "TransactionId" → "transaction_id"
    private static string PascalToSnakeCase(string name)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
                sb.Append('_');
            sb.Append(char.ToLower(name[i]));
        }
        return sb.ToString();
    }

    public void Draw(SKCanvas canvas, int width, int height)
    {
        canvas.Clear(CBg);

        // Card
        using var rrCard      = new SKRoundRect(new SKRect(CardM, CardM, width - CardM, height - CardM), 10f);
        using var pCardFill   = new SKPaint { Color = CCard,   IsAntialias = true };
        using var pCardStroke = new SKPaint { Color = CBorder, Style = SKPaintStyle.Stroke, StrokeWidth = 1f, IsAntialias = true };
        canvas.DrawRoundRect(rrCard, pCardFill);
        canvas.DrawRoundRect(rrCard, pCardStroke);

        // Fonts
        using var tfReg  = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal);
        using var tfBold = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold);
        using var fTitle = new SKFont(tfBold, 15f);
        using var fSmall = new SKFont(tfReg,  12f);
        using var fRow   = new SKFont(tfReg,  13f);
        using var fRowB  = new SKFont(tfBold, 13f);

        using var pPrim  = new SKPaint { Color = CPrim,    IsAntialias = true };
        using var pMuted = new SKPaint { Color = CMuted,   IsAntialias = true };
        using var pDiv   = new SKPaint { Color = CDivider, StrokeWidth = 1f };

        // --- Dynamic column layout ---
        // Measure max content width per column, then scale proportionally to fill available space.
        float availW = width - LeftX - DotsW - CardM;
        float[] rawW = new float[_columnKeys.Length];
        for (int c = 0; c < _columnKeys.Length; c++)
        {
            rawW[c] = fSmall.MeasureText(_columnHeaders[c]);
            bool isStatus = _columnKeys[c].Equals("status", StringComparison.OrdinalIgnoreCase);
            foreach (var row in _rows)
            {
                float cellW = isStatus
                    ? fSmall.MeasureText(row[c]) + 24f   // badge has extra padding
                    : fRow.MeasureText(row[c]);
                rawW[c] = Math.Max(rawW[c], cellW);
            }
            rawW[c] += 24f; // inter-column gap
        }

        float totalRaw = rawW.Sum();
        float[] colW = rawW.Select(w => w / totalRaw * availW).ToArray();
        float[] colX = new float[_columnKeys.Length];
        colX[0] = LeftX;
        for (int c = 1; c < _columnKeys.Length; c++)
            colX[c] = colX[c - 1] + colW[c - 1];

        // Header title
        canvas.DrawText(_title, LeftX, 56f, fTitle, pPrim);

        // Export button
        const float btnW = 108f, btnH = 32f;
        float btnX = width - CardM - btnW;
        using var rrBtn = new SKRoundRect(new SKRect(btnX, 36f, btnX + btnW, 36f + btnH), btnH / 2f);
     

        canvas.DrawLine(CardM, 83f, width - CardM, 83f, pDiv);

        // Column headers
        for (int c = 0; c < _columnHeaders.Length; c++)
        {
            bool isAmount = _columnKeys[c].Equals("amount", StringComparison.OrdinalIgnoreCase);
            if (isAmount)
            {
                float hw = fSmall.MeasureText(_columnHeaders[c]);
                canvas.DrawText(_columnHeaders[c], colX[c] + colW[c] - hw, 108f, fSmall, pMuted);
            }
            else
            {
                canvas.DrawText(_columnHeaders[c], colX[c], 108f, fSmall, pMuted);
            }
        }

        // Badge reuse paints
        using var pBadgeFill   = new SKPaint { IsAntialias = true };
        using var pBadgeStroke = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 1f, IsAntialias = true };
        using var pBadgeText   = new SKPaint { IsAntialias = true };

        // Data rows
        for (int i = 0; i < _rows.Length; i++)
        {
            var row = _rows[i];
            float ry = RowsTop + RowH * i;
            float cy = ry + RowH / 2f;
            float ty = cy + 5f;

            canvas.DrawLine(CardM, ry, width - CardM, ry, pDiv);

            for (int c = 0; c < _columnKeys.Length; c++)
            {
                string key = _columnKeys[c];
                string val = row[c];
                bool isStatus = key.Equals("status", StringComparison.OrdinalIgnoreCase);
                bool isAmount = key.Equals("amount", StringComparison.OrdinalIgnoreCase);

                if (isStatus)
                {
                    var colors = BadgeColors.TryGetValue(val, out var bc) ? bc : BadgeFallback;
                    const float bPad = 12f, bH = 26f;
                    float bW = fSmall.MeasureText(val) + bPad * 2f;
                    using var rrBadge = new SKRoundRect(
                        new SKRect(colX[c], cy - bH / 2f, colX[c] + bW, cy + bH / 2f), bH / 2f);
                    pBadgeFill.Color   = colors.bg;
                    pBadgeStroke.Color = colors.border;
                    pBadgeText.Color   = colors.text;
                    canvas.DrawRoundRect(rrBadge, pBadgeFill);
                    canvas.DrawRoundRect(rrBadge, pBadgeStroke);
                    canvas.DrawText(val, colX[c] + bPad, cy + 4f, fSmall, pBadgeText);
                }
                else if (isAmount)
                {
                    float aW = fRow.MeasureText(val);
                    canvas.DrawText(val, colX[c] + colW[c] - aW, ty, fRow, pPrim);
                }
                else
                {
                    // Column 0 = muted ID; all others = bold white
                    canvas.DrawText(val, colX[c], ty, c == 0 ? fRow : fRowB, c == 0 ? pMuted : pPrim);
                }
            }

            canvas.DrawText("···", width - DotsW, ty, fRow, pMuted);
        }
    }
}
