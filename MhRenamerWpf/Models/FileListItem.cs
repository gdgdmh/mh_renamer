using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MhRenamerWpf.Models;

public sealed class FileListItem : INotifyPropertyChanged
{
    private string _name;
    private string _previewName = "";
    private string _fullPath;

    public FileListItem(string name, string fullPath, string size, string fileType, bool isDirectory)
    {
        _name = name;
        _fullPath = fullPath;
        Size = size;
        FileType = fileType;
        IsDirectory = isDirectory;
    }

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public string PreviewName
    {
        get => _previewName;
        set => SetField(ref _previewName, value);
    }

    public string FullPath
    {
        get => _fullPath;
        set => SetField(ref _fullPath, value);
    }

    public string Size { get; }
    public string FileType { get; }
    public bool IsDirectory { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
