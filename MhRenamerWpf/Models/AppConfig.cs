namespace MhRenamerWpf.Models;

public sealed class AppConfig
{
    public string LastDirectory { get; set; } = "";
    public bool DefaultAllSelect { get; set; } = true;
    public int StartNum { get; set; }
    public int Digits { get; set; } = 5;
}
