using System.Drawing;

namespace BaumKeyGenerator;

internal static class AppTheme
{
    // Backgrounds
    public static readonly Color Background    = Color.FromArgb(18, 18, 24);
    public static readonly Color Surface       = Color.FromArgb(26, 26, 36);
    public static readonly Color SurfaceAlt    = Color.FromArgb(34, 34, 48);
    public static readonly Color Border        = Color.FromArgb(55, 55, 75);

    // Accents
    public static readonly Color Accent        = Color.FromArgb(99, 102, 241);   // indigo
    public static readonly Color AccentHover   = Color.FromArgb(129, 132, 255);
    public static readonly Color AccentPressed = Color.FromArgb(79,  82, 210);
    public static readonly Color Success       = Color.FromArgb(34,  197, 94);
    public static readonly Color Danger        = Color.FromArgb(239, 68,  68);

    // Text
    public static readonly Color TextPrimary   = Color.FromArgb(230, 230, 245);
    public static readonly Color TextSecondary = Color.FromArgb(148, 148, 175);
    public static readonly Color TextMuted     = Color.FromArgb(90,  90, 120);

    // Fonts
    public static readonly Font FontBase       = new("Segoe UI",        10f, FontStyle.Regular);
    public static readonly Font FontSmall      = new("Segoe UI",         9f, FontStyle.Regular);
    public static readonly Font FontLabel      = new("Segoe UI",         9f, FontStyle.Bold);
    public static readonly Font FontMono       = new("Cascadia Mono",   10f, FontStyle.Regular);
    public static readonly Font FontMonoSmall  = new("Cascadia Mono",    8f, FontStyle.Regular);
    public static readonly Font FontTitle      = new("Segoe UI",        14f, FontStyle.Bold);

    public static void ApplyToForm(Form form)
    {
        form.BackColor = Background;
        form.ForeColor = TextPrimary;
        form.Font      = FontBase;
    }

    public static void Style(Label label, bool secondary = false)
    {
        label.ForeColor = secondary ? TextSecondary : TextPrimary;
        label.BackColor = Color.Transparent;
        label.Font      = FontLabel;
    }

    public static void Style(TextBox tb, bool mono = false)
    {
        tb.BackColor  = SurfaceAlt;
        tb.ForeColor  = TextPrimary;
        tb.BorderStyle = BorderStyle.FixedSingle;
        tb.Font       = mono ? FontMono : FontBase;
    }

    public static void Style(ComboBox cb)
    {
        cb.BackColor  = SurfaceAlt;
        cb.ForeColor  = TextPrimary;
        cb.FlatStyle  = FlatStyle.Flat;
        cb.Font       = FontBase;
    }

    public static void StylePrimary(Button btn)
    {
        btn.BackColor   = Accent;
        btn.ForeColor   = Color.White;
        btn.FlatStyle   = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.Font        = FontBase;
        btn.Cursor      = Cursors.Hand;
        btn.MouseEnter += (_, _) => btn.BackColor = AccentHover;
        btn.MouseLeave += (_, _) => btn.BackColor = Accent;
        btn.MouseDown  += (_, _) => btn.BackColor = AccentPressed;
        btn.MouseUp    += (_, _) => btn.BackColor = Accent;
    }

    public static void StyleSecondary(Button btn)
    {
        btn.BackColor   = SurfaceAlt;
        btn.ForeColor   = TextPrimary;
        btn.FlatStyle   = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize  = 1;
        btn.FlatAppearance.BorderColor = Border;
        btn.Font        = FontBase;
        btn.Cursor      = Cursors.Hand;
        btn.MouseEnter += (_, _) => btn.BackColor = Surface;
        btn.MouseLeave += (_, _) => btn.BackColor = SurfaceAlt;
    }

    public static void StyleDanger(Button btn)
    {
        btn.BackColor   = Color.FromArgb(80, 30, 30);
        btn.ForeColor   = Color.FromArgb(255, 120, 120);
        btn.FlatStyle   = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.Font        = FontSmall;
        btn.Cursor      = Cursors.Hand;
        btn.MouseEnter += (_, _) => btn.BackColor = Color.FromArgb(100, 30, 30);
        btn.MouseLeave += (_, _) => btn.BackColor = Color.FromArgb(80, 30, 30);
    }

    public static Panel MakeDivider()
    {
        return new Panel
        {
            Height    = 1,
            Dock      = DockStyle.Top,
            BackColor = Border,
        };
    }
}
