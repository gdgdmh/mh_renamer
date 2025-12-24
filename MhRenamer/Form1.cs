
using System.Runtime.InteropServices;
namespace MhRenamer;

public partial class Form1 : Form
{
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_SMALLICON = 0x1;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x10;
    private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

    public Form1()
    {
        InitializeComponent();

        treeView1.ImageList = CreateImageList();
        treeView1.BeforeExpand += TreeView_BeforeExpand;
        treeView1.AfterSelect += TreeView_AfterSelect;

        // デスクトップを最初に追加
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var desktopNode = new TreeNode("デスクトップ")
            {
                Tag = desktopPath,
                ImageIndex = 0,
                SelectedImageIndex = 0
            };
            desktopNode.Nodes.Add(""); // 展開可能にするためのダミー
            treeView1.Nodes.Add(desktopNode);
        }
        // ドライブを追加
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            var node = new TreeNode(drive.Name)
            {
                Tag = drive.RootDirectory.FullName,
                ImageIndex = 0,
                SelectedImageIndex = 0
            };
            node.Nodes.Add("");  // 展開可能にするためのダミー
            treeView1.Nodes.Add(node);
        }

        fileListView.Dock = DockStyle.None;
        fileListView.View = View.Details;   // 詳細表示モード
        fileListView.FullRowSelect = true; // 行全体を選択
        fileListView.GridLines = true;      // グリッド線を表示
        fileListView.SmallImageList = CreateImageList();

        fileListView.Columns.Add("名前", 200);
        fileListView.Columns.Add("変更後ファイル名", 200);
        fileListView.Columns.Add("サイズ", 80, HorizontalAlignment.Right);
        fileListView.Columns.Add("ファイルの種類", 100);
        
        
    }

    private ImageList CreateImageList()
    {
        var imageList = new ImageList();
        imageList.ImageSize = new Size(16, 16);
        imageList.ColorDepth = ColorDepth.Depth32Bit;

        // フォルダアイコンを取得
        var folderIcon = GetSystemIcon(FILE_ATTRIBUTE_DIRECTORY);
        if (folderIcon != null)
        {
            imageList.Images.Add(folderIcon);  // index 0: フォルダ
        }

        // ファイルアイコンを取得
        var fileIcon = GetSystemIcon(FILE_ATTRIBUTE_NORMAL);
        if (fileIcon != null)
        {
            imageList.Images.Add(fileIcon);  // index 1: ファイル
        }

        return imageList;
    }
    
    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private static Icon? GetSystemIcon(uint fileAttribute)
    {
        var shinfo = new SHFILEINFO();
        var result = SHGetFileInfo(
            "dummy",  // ダミーパス（属性で判断させる）
            fileAttribute,
            ref shinfo,
            (uint)Marshal.SizeOf(shinfo),
            SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES);

        if (result == IntPtr.Zero || shinfo.hIcon == IntPtr.Zero)
        {
            return null;
        }

        var icon = Icon.FromHandle(shinfo.hIcon).Clone() as Icon;
        DestroyIcon(shinfo.hIcon);  // リソース解放
        return icon;
    }
    private void TreeView_BeforeExpand(object? sender, TreeViewCancelEventArgs e)
    {
        var node = e.Node;
        if (node == null) return;

        if (node.Nodes.Count == 1 && node.Nodes[0].Text == "")
        {
            node.Nodes.Clear();
            var path = node.Tag as string;
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                foreach (var dir in Directory.GetDirectories(path))
                {
                    var child = new TreeNode(Path.GetFileName(dir))
                    {
                        Tag = dir,
                        ImageIndex = 0,
                        SelectedImageIndex = 0
                    };
                    child.Nodes.Add("");
                    node.Nodes.Add(child);
                }

                // treeviewにはファイルはいったんなし
                /*
                foreach (var file in Directory.GetFiles(path))
                {
                    node.Nodes.Add(new TreeNode(Path.GetFileName(file))
                    {
                        Tag = file,
                        ImageIndex = 1,
                        SelectedImageIndex = 1
                    });
                }
                */
            }
            catch (UnauthorizedAccessException) { }
        }
    }

    private async void TreeView_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (e.Node?.Tag is string path && Directory.Exists(path))
        {
            await LoadFilesToListViewAsync(path);
        }
    }

    private CancellationTokenSource? _loadCancellation;
    private async Task LoadFilesToListViewAsync(string directoryPath)
    {
        // 前回の読み込みをキャンセル
        _loadCancellation?.Cancel();
        _loadCancellation = new CancellationTokenSource();
        var token = _loadCancellation.Token;

        fileListView.Items.Clear();
        fileListView.Cursor = Cursors.WaitCursor;  // 読み込み中カーソル

        try
        {
            // バックグラウンドでファイル情報を取得
            var fileItems = await Task.Run(() =>
            {
                var items = new List<ListViewItem>();

                foreach (var file in Directory.GetFiles(directoryPath))
                {
                    token.ThrowIfCancellationRequested();

                    var fileInfo = new FileInfo(file);
                    var item = new ListViewItem(fileInfo.Name)
                    {
                        ImageIndex = 1,
                        Tag = fileInfo.FullName
                    };

                    item.SubItems.Add("");  // 変更後ファイル名
                    item.SubItems.Add(FormatFileSize(fileInfo.Length));
                    item.SubItems.Add(GetFileType(fileInfo.Extension));

                    items.Add(item);
                }

                return items;
            }, token);

            // キャンセルされていなければUIに反映
            if (!token.IsCancellationRequested)
            {
                fileListView.BeginUpdate();  // 描画を一時停止（高速化）
                fileListView.Items.AddRange(fileItems.ToArray());
                fileListView.EndUpdate();
            }
        }
        catch (OperationCanceledException)
        {
            // キャンセルされた場合は何もしない
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
        finally
        {
            fileListView.Cursor = Cursors.Default;
        }
    }

    private void LoadFilesToListView(string directoryPath)
    {
        fileListView.Items.Clear();

        try
        {
            foreach (var file in Directory.GetFiles(directoryPath))
            {
                var fileInfo = new FileInfo(file);
                var item = new ListViewItem(fileInfo.Name)
                {
                    ImageIndex = 1,  // ファイルアイコン
                    Tag = fileInfo.FullName
                };

                item.SubItems.Add("");  // 変更後ファイル名（空欄）
                item.SubItems.Add(FormatFileSize(fileInfo.Length));
                item.SubItems.Add(GetFileType(fileInfo.Extension));

                fileListView.Items.Add(item);
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
    }

    private string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / 1024 / 1024} MB";
        return $"{bytes / 1024 / 1024 / 1024} GB";
    }

    private string GetFileType(string extension)
    {
        if (string.IsNullOrEmpty(extension)) return "ファイル";
        return $"{extension.ToUpper().TrimStart('.')} ファイル";
    }
    
    
    
}
