using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MhRenamerWpf.Models;
using MhRenamerWpf.Services;

namespace MhRenamerWpf;

public partial class MainWindow : Window
{
    private const int MinimumStartNumber = 0;
    private const int MaximumStartNumber = 10_000_000;
    private const int MinimumDigits = 1;
    private const int MaximumDigits = 100;

    private CancellationTokenSource? _loadCancellation;
    private string _currentDirectoryPath = "";
    private bool _isInitializing;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    public ObservableCollection<FolderTreeItem> RootFolders { get; } = [];
    public ObservableCollection<FileListItem> FileItems { get; } = [];

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _isInitializing = true;

        var config = AppConfigManager.Load();
        StartNumberTextBox.Text = Math.Clamp(
            config.StartNum,
            MinimumStartNumber,
            MaximumStartNumber).ToString(CultureInfo.CurrentCulture);
        DigitsTextBox.Text = Math.Clamp(
            config.Digits,
            MinimumDigits,
            MaximumDigits).ToString(CultureInfo.CurrentCulture);
        DefaultAllSelectCheckBox.IsChecked = config.DefaultAllSelect;

        InitializeFolderTree();

        var restorePath = Directory.Exists(config.LastDirectory)
            ? config.LastDirectory
            : Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        SelectFolderPath(restorePath);
        _isInitializing = false;
        await LoadFilesAsync(restorePath);
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        _loadCancellation?.Cancel();

        AppConfigManager.Save(new AppConfig
        {
            LastDirectory = _currentDirectoryPath,
            DefaultAllSelect = DefaultAllSelectCheckBox.IsChecked == true,
            StartNum = GetStartNumber(),
            Digits = GetDigits()
        });
    }

    private void InitializeFolderTree()
    {
        RootFolders.Clear();

        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        if (Directory.Exists(desktopPath))
        {
            RootFolders.Add(new FolderTreeItem("デスクトップ", desktopPath));
        }

        foreach (var drive in DriveInfo.GetDrives().Where(drive => drive.IsReady))
        {
            RootFolders.Add(new FolderTreeItem(drive.Name, drive.RootDirectory.FullName));
        }
    }

    private void SelectFolderPath(string targetPath)
    {
        var normalizedTarget = Path.GetFullPath(targetPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var root = RootFolders
            .Where(item => IsSameOrChildPath(normalizedTarget, item.FullPath))
            .OrderByDescending(item => item.FullPath.Length)
            .FirstOrDefault();

        if (root is null)
        {
            return;
        }

        var current = root;
        current.IsExpanded = true;

        while (!PathsEqual(current.FullPath, normalizedTarget))
        {
            current.LoadChildren();

            var next = current.Children
                .Where(item => !item.IsPlaceholder && IsSameOrChildPath(normalizedTarget, item.FullPath))
                .OrderByDescending(item => item.FullPath.Length)
                .FirstOrDefault();

            if (next is null)
            {
                break;
            }

            current = next;
            current.IsExpanded = true;
        }

        current.IsSelected = true;
    }

    private void FolderTreeItem_Expanded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TreeViewItem { DataContext: FolderTreeItem folder })
        {
            folder.LoadChildren();
        }
    }

    private async void FolderTreeView_SelectedItemChanged(
        object sender,
        RoutedPropertyChangedEventArgs<object> e)
    {
        if (_isInitializing || e.NewValue is not FolderTreeItem { IsPlaceholder: false } folder)
        {
            return;
        }

        await LoadFilesAsync(folder.FullPath);
    }

    private async Task LoadFilesAsync(string directoryPath)
    {
        _currentDirectoryPath = directoryPath;
        CurrentDirectoryTextBlock.Text = directoryPath;

        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = new CancellationTokenSource();
        var token = _loadCancellation.Token;

        FileItems.Clear();
        Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

        try
        {
            var loadedItems = await Task.Run(() => ReadDirectory(directoryPath, token), token);
            token.ThrowIfCancellationRequested();

            foreach (var item in loadedItems)
            {
                FileItems.Add(item);
            }

            if (DefaultAllSelectCheckBox.IsChecked == true)
            {
                FileListView.SelectAll();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show(
                this,
                "このフォルダへアクセスする権限がありません。",
                "アクセスエラー",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (IOException exception)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "読み込みエラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                Mouse.OverrideCursor = null;
            }
        }
    }

    private static List<FileListItem> ReadDirectory(string directoryPath, CancellationToken token)
    {
        var items = new List<FileListItem>();

        foreach (var directory in Directory.EnumerateDirectories(directoryPath)
                     .OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase))
        {
            token.ThrowIfCancellationRequested();
            var directoryInfo = new DirectoryInfo(directory);
            items.Add(new FileListItem(
                directoryInfo.Name,
                directoryInfo.FullName,
                "",
                "フォルダー",
                true));
        }

        foreach (var file in Directory.EnumerateFiles(directoryPath)
                     .OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase))
        {
            token.ThrowIfCancellationRequested();
            var fileInfo = new FileInfo(file);
            items.Add(new FileListItem(
                fileInfo.Name,
                fileInfo.FullName,
                FormatFileSize(fileInfo.Length),
                GetFileType(fileInfo.Extension),
                false));
        }

        return items;
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024} KB";
        }

        if (bytes < 1024L * 1024 * 1024)
        {
            return $"{bytes / 1024 / 1024} MB";
        }

        return $"{bytes / 1024 / 1024 / 1024} GB";
    }

    private static string GetFileType(string extension) =>
        string.IsNullOrEmpty(extension)
            ? "ファイル"
            : $"{extension.TrimStart('.').ToUpperInvariant()} ファイル";

    private void FileListView_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        UpdateRenamePreview();

    private void RenameSettingTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded)
        {
            UpdateRenamePreview();
        }
    }

    private void UpdateRenamePreview()
    {
        foreach (var item in FileItems)
        {
            item.PreviewName = "";
        }

        var startNumber = GetStartNumber();
        var digits = GetDigits();
        var selectedItems = FileItems
            .Where(item => FileListView.SelectedItems.Contains(item))
            .ToList();

        for (var index = 0; index < selectedItems.Count; index++)
        {
            var numberText = (startNumber + index).ToString(CultureInfo.CurrentCulture);
            selectedItems[index].PreviewName = numberText.PadLeft(digits, '0');
        }
    }

    private void SelectAllButton_Click(object sender, RoutedEventArgs e) =>
        FileListView.SelectAll();

    private void StartNumberUpButton_Click(object sender, RoutedEventArgs e) =>
        SetStartNumber(GetStartNumber() + 1);

    private void StartNumberDownButton_Click(object sender, RoutedEventArgs e) =>
        SetStartNumber(GetStartNumber() - 1);

    private void DigitsUpButton_Click(object sender, RoutedEventArgs e) =>
        SetDigits(GetDigits() + 1);

    private void DigitsDownButton_Click(object sender, RoutedEventArgs e) =>
        SetDigits(GetDigits() - 1);

    private int GetStartNumber() =>
        ParseClampedValue(
            StartNumberTextBox?.Text,
            MinimumStartNumber,
            MaximumStartNumber,
            MinimumStartNumber);

    private int GetDigits() =>
        ParseClampedValue(
            DigitsTextBox?.Text,
            MinimumDigits,
            MaximumDigits,
            MinimumDigits);

    private void SetStartNumber(int value) =>
        StartNumberTextBox.Text = Math.Clamp(
            value,
            MinimumStartNumber,
            MaximumStartNumber).ToString(CultureInfo.CurrentCulture);

    private void SetDigits(int value) =>
        DigitsTextBox.Text = Math.Clamp(
            value,
            MinimumDigits,
            MaximumDigits).ToString(CultureInfo.CurrentCulture);

    private static int ParseClampedValue(string? text, int minimum, int maximum, int fallback)
    {
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var value)
            ? Math.Clamp(value, minimum, maximum)
            : fallback;
    }

    private void RenameButton_Click(object sender, RoutedEventArgs e)
    {
        var targets = FileItems
            .Where(item =>
                FileListView.SelectedItems.Contains(item) &&
                !string.IsNullOrWhiteSpace(item.PreviewName))
            .ToList();

        if (targets.Count == 0)
        {
            MessageBox.Show(
                this,
                "リネーム対象がありません。\nファイルまたはフォルダを選択してください。",
                "確認",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var renameEntries = targets
            .Select(item =>
            {
                var directory = Path.GetDirectoryName(item.FullPath) ?? "";
                var newName = item.PreviewName + Path.GetExtension(item.FullPath);
                return new
                {
                    Item = item,
                    OldPath = item.FullPath,
                    NewName = newName,
                    NewPath = Path.Combine(directory, newName)
                };
            })
            .ToList();

        var result = RenameService.Execute(
            renameEntries
                .Select(entry => new RenameRequest(entry.OldPath, entry.NewPath))
                .ToList());

        if (!result.Succeeded)
        {
            MessageBox.Show(
                this,
                result.ErrorMessage,
                "リネームエラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        foreach (var entry in renameEntries)
        {
            entry.Item.Name = entry.NewName;
            entry.Item.FullPath = entry.NewPath;
            entry.Item.PreviewName = "";
        }
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);

    private static bool IsSameOrChildPath(string targetPath, string parentPath)
    {
        var normalizedTarget = Path.GetFullPath(targetPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedParent = Path.GetFullPath(parentPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return PathsEqual(normalizedTarget, normalizedParent) ||
               normalizedTarget.StartsWith(
                   normalizedParent + Path.DirectorySeparatorChar,
                   StringComparison.OrdinalIgnoreCase);
    }
}
