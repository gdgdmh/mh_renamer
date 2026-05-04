namespace MhRenamer;

// アプリのテーマ用
public class AppTheme
{
    public Color BackColor { get; init; }
    public Color ForeColor { get; init; }
    public Color ControlBackColor { get; init; }
    public Color ControlForeColor { get; init; }
    public Color BorderColor { get; init; }

    public static AppTheme Light => new()
    {
        BackColor = Color.White,
        ForeColor = Color.Black,
        ControlBackColor = Color.White,
        ControlForeColor = Color.Black,
        BorderColor = Color.FromArgb(200, 200, 200)
    };

    public static AppTheme Dark => new()
    {
        BackColor = Color.FromArgb(32, 32, 32),
        ForeColor = Color.White,
        ControlBackColor = Color.FromArgb(45, 45, 45),
        ControlForeColor = Color.White,
        BorderColor = Color.FromArgb(60, 60, 60)
    };
}