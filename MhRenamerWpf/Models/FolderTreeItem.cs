using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace MhRenamerWpf.Models;

public sealed class FolderTreeItem : INotifyPropertyChanged
{
    private static readonly FolderTreeItem Placeholder = new("", "", true);
    private bool _childrenLoaded;
    private bool _isExpanded;
    private bool _isSelected;

    public FolderTreeItem(string name, string fullPath)
        : this(name, fullPath, false)
    {
        Children.Add(Placeholder);
    }

    private FolderTreeItem(string name, string fullPath, bool isPlaceholder)
    {
        Name = name;
        FullPath = fullPath;
        IsPlaceholder = isPlaceholder;
    }

    public string Name { get; }
    public string FullPath { get; }
    public bool IsPlaceholder { get; }
    public ObservableCollection<FolderTreeItem> Children { get; } = [];

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetField(ref _isExpanded, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    public void LoadChildren()
    {
        if (_childrenLoaded || IsPlaceholder)
        {
            return;
        }

        _childrenLoaded = true;
        Children.Clear();

        try
        {
            foreach (var directoryPath in Directory.EnumerateDirectories(FullPath)
                         .OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase))
            {
                Children.Add(new FolderTreeItem(Path.GetFileName(directoryPath), directoryPath));
            }
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (IOException)
        {
        }
    }

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
