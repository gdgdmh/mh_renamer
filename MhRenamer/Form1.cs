
using System.Runtime.InteropServices;
namespace MhRenamer;

public partial class Form1 : Form
{
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref Shfileinfo psfi,
        uint cbFileInfo,
        uint uFlags);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct Shfileinfo
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    private const uint ShgfiIcon = 0x100;
    private const uint ShgfiSmallicon = 0x1;
    private const uint ShgfiUsefileattributes = 0x10;
    private const uint FileAttributeDirectory = 0x10;
    private const uint FileAttributeNormal = 0x80;

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
            if (HasSubdirectories(desktopPath))
            {
                desktopNode.Nodes.Add("");
            }
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
            if (HasSubdirectories(drive.RootDirectory.FullName))
            {
                node.Nodes.Add("");
            }
            treeView1.Nodes.Add(node);
        }

        fileListView.Dock = DockStyle.None;
        fileListView.View = View.Details;   // 詳細表示モード
        fileListView.FullRowSelect = true; // 行全体を選択
        fileListView.GridLines = true;      // グリッド線を表示
        fileListView.MultiSelect = true;    // 複数選択可能
        fileListView.SmallImageList = CreateImageList();

        fileListView.Columns.Add("名前", 200);
        fileListView.Columns.Add("変更後ファイル名", 200);
        fileListView.Columns.Add("サイズ", 80, HorizontalAlignment.Right);
        fileListView.Columns.Add("ファイルの種類", 100);
        
        // 選択変更・設定変更でプレビューを更新
        fileListView.ItemSelectionChanged += (s, e) => UpdateRenamePreview();
        startNumNumericUpDown.ValueChanged += (s, e) => UpdateRenamePreview();
        digitNumericUpDown1.ValueChanged += (s, e) => UpdateRenamePreview();
        
        // 全選択ボタン
        allSelectButton.Click += (s, e) => fileListView.Items.Cast<ListViewItem>().ToList().ForEach(item => item.Selected = true);
        
        // リネームボタン
        renameButton.Click += (s, e) => ExecuteRename();
        
        this.Load += (s, e) => ApplyTheme(true);
    }

    private ImageList CreateImageList()
    {
        var imageList = new ImageList();
        imageList.ImageSize = new Size(16, 16);
        imageList.ColorDepth = ColorDepth.Depth32Bit;

        // フォルダアイコンを取得
        var folderIcon = GetSystemIcon(FileAttributeDirectory);
        if (folderIcon != null)
        {
            imageList.Images.Add(folderIcon);  // index 0: フォルダ
        }

        // ファイルアイコンを取得
        var fileIcon = GetSystemIcon(FileAttributeNormal);
        if (fileIcon != null)
        {
            imageList.Images.Add(fileIcon);  // index 1: ファイル
        }

        return imageList;
    }
    
    // システムアイコンの取得
    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private static Icon? GetSystemIcon(uint fileAttribute)
    {
        var shinfo = new Shfileinfo();
        var result = SHGetFileInfo(
            "dummy",  // ダミーパス（属性で判断させる）
            fileAttribute,
            ref shinfo,
            (uint)Marshal.SizeOf(shinfo),
            ShgfiIcon | ShgfiSmallicon | ShgfiUsefileattributes);

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

                    // サブフォルダがある場合のみダミーを追加
                    if (HasSubdirectories(dir))
                    {
                        child.Nodes.Add("");
                    }

                    node.Nodes.Add(child);
                }
            }
            catch (UnauthorizedAccessException) { }
        }
    }

    private async void TreeView_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        try
        {
            if (e.Node?.Tag is string path && Directory.Exists(path))
            {
                await LoadFilesToListViewAsync(path);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("エラーが発生しました: " + ex.Message, "エラー", MessageBoxButtons.OK);
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
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

    private bool HasSubdirectories(string path)
    {
        try
        {
            return Directory.EnumerateDirectories(path).Any();
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private void UpdateRenamePreview()
    {
        var startNum = (int)startNumNumericUpDown.Value;
        var digits   = (int)digitNumericUpDown1.Value;

        // 全行のプレビューをいったんクリア
        foreach (ListViewItem item in fileListView.Items)
        {
            item.SubItems[1].Text = "";
        }

        // 選択行を表示順（Index順）で取得し連番を設定
        var selectedItems = fileListView.SelectedItems
            .Cast<ListViewItem>()
            .OrderBy(item => item.Index)
            .ToList();

        for (int i = 0; i < selectedItems.Count; i++)
        {
            var num = startNum + i;
            selectedItems[i].SubItems[1].Text = num.ToString().PadLeft(digits, '0');
        }
    }

    // リネーム実行
    private void ExecuteRename()
    {
        // 変更後ファイル名が設定されている行のみ対象
        var targets = fileListView.SelectedItems
            .Cast<ListViewItem>()
            .Where(item => !string.IsNullOrEmpty(item.SubItems[1].Text))
            .OrderBy(item => item.Index)
            .ToList();

        if (targets.Count == 0)
        {
            MessageBox.Show("リネーム対象がありません。\nファイルを選択してください。",
                "確認", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // リネーム計画を作成（元パス → 新パス）
        var renamePlan = new List<(string OldPath, string NewPath, string NewName)>();

        foreach (var item in targets)
        {
            var oldPath   = item.Tag as string;
            var newBaseName = item.SubItems[1].Text;

            if (string.IsNullOrEmpty(oldPath)) continue;

            var dir       = Path.GetDirectoryName(oldPath) ?? "";
            var extension = Path.GetExtension(oldPath);       // 元の拡張子をそのまま使用
            var newName   = newBaseName + extension;
            var newPath   = Path.Combine(dir, newName);

            renamePlan.Add((oldPath, newPath, newName));
        }

        // ── 事前チェック1: 変更後ファイル名の重複（計画内での重複） ──
        var duplicatesInPlan = renamePlan
            .GroupBy(r => r.NewPath, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicatesInPlan.Count > 0)
        {
            var names = string.Join("\n", duplicatesInPlan.Select(p => Path.GetFileName(p)));
            MessageBox.Show(
                $"リネーム後に名前が重複するファイルがあります。処理を中断します。\n\n{names}",
                "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // ── 事前チェック2: 既存ファイルとの衝突 ──
        // （元のファイル自身への上書きは除外してチェック）
        var oldPathSet = new HashSet<string>(
            renamePlan.Select(r => r.OldPath), StringComparer.OrdinalIgnoreCase);

        var conflicts = renamePlan
            .Where(r => File.Exists(r.NewPath) && !oldPathSet.Contains(r.NewPath))
            .Select(r => r.NewName)
            .ToList();

        if (conflicts.Count > 0)
        {
            var names = string.Join("\n", conflicts);
            MessageBox.Show(
                $"以下のファイルは既に存在するためリネームできません。処理を中断します。\n\n{names}",
                "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // ── リネーム実行 ──
        var renamedItems = new List<(ListViewItem Item, string NewName)>();

        foreach (var (oldPath, newPath, newName) in renamePlan)
        {
            try
            {
                File.Move(oldPath, newPath);

                // 対応する ListViewItem を記録（後でUI更新用）
                var item = fileListView.Items
                    .Cast<ListViewItem>()
                    .FirstOrDefault(i => string.Equals(i.Tag as string, oldPath,
                        StringComparison.OrdinalIgnoreCase));

                if (item != null)
                {
                    renamedItems.Add((item, newName));
                }
            }
            catch (Exception ex)
            {
                // 失敗したファイル名とエラー内容を表示して中断
                MessageBox.Show(
                    $"リネームに失敗しました。処理を中断します。\n\nファイル: {Path.GetFileName(oldPath)}\nエラー: {ex.Message}",
                    "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        // ── UI更新：成功したアイテムのみ反映 ──
        fileListView.BeginUpdate();
        foreach (var (item, newName) in renamedItems)
        {
            var dir     = Path.GetDirectoryName(item.Tag as string) ?? "";
            var newPath = Path.Combine(dir, newName);

            item.Text            = newName;        // 名前列
            item.SubItems[1].Text = "";            // 変更後ファイル名列をクリア
            item.Tag             = newPath;        // Tagのパスも更新
        }
        fileListView.EndUpdate();
    }


    // Win32 API（タイトルバーのダークモード用）
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private bool _isDarkMode = false;

    // テーマの適用(ダークモード)
    private void ApplyTheme(bool darkMode)
    {
        _isDarkMode = darkMode;
        var theme = darkMode ? AppTheme.Dark : AppTheme.Light;

        // フォーム
        this.BackColor = theme.BackColor;
        this.ForeColor = theme.ForeColor;

        // タイトルバーのダークモード（Windows 10 20H1以降）
        int useDarkMode = darkMode ? 1 : 0;
        DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));

        // TreeView
        treeView1.BackColor = theme.ControlBackColor;
        treeView1.ForeColor = theme.ControlForeColor;

        // ListView
        fileListView.BackColor = theme.ControlBackColor;
        fileListView.ForeColor = theme.ControlForeColor;

        // 再描画
        this.Refresh();
    }
}
