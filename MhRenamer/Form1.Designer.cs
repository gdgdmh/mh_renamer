namespace MhRenamer;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
        treeView1 = new System.Windows.Forms.TreeView();
        fileListView = new System.Windows.Forms.ListView();
        renameSettingLabel = new System.Windows.Forms.Label();
        label1 = new System.Windows.Forms.Label();
        digitNumericUpDown1 = new System.Windows.Forms.NumericUpDown();
        startNumNumericUpDown = new System.Windows.Forms.NumericUpDown();
        defaultAllSelectCheckBox = new System.Windows.Forms.CheckBox();
        renameButton = new System.Windows.Forms.Button();
        allSelectButton = new System.Windows.Forms.Button();
        ((System.ComponentModel.ISupportInitialize)digitNumericUpDown1).BeginInit();
        ((System.ComponentModel.ISupportInitialize)startNumNumericUpDown).BeginInit();
        SuspendLayout();
        // 
        // treeView1
        // 
        treeView1.HideSelection = false;
        treeView1.Location = new System.Drawing.Point(12, 12);
        treeView1.Name = "treeView1";
        treeView1.Size = new System.Drawing.Size(340, 481);
        treeView1.TabIndex = 0;
        // 
        // fileListView
        // 
        fileListView.Location = new System.Drawing.Point(358, 12);
        fileListView.Name = "fileListView";
        fileListView.Size = new System.Drawing.Size(1038, 481);
        fileListView.TabIndex = 1;
        fileListView.UseCompatibleStateImageBehavior = false;
        // 
        // renameSettingLabel
        // 
        renameSettingLabel.Font = new System.Drawing.Font("Yu Gothic UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)128));
        renameSettingLabel.Location = new System.Drawing.Point(1462, 57);
        renameSettingLabel.Name = "renameSettingLabel";
        renameSettingLabel.Size = new System.Drawing.Size(100, 23);
        renameSettingLabel.TabIndex = 2;
        renameSettingLabel.Text = "リネーム設定";
        // 
        // label1
        // 
        label1.Location = new System.Drawing.Point(1402, 96);
        label1.Name = "label1";
        label1.Size = new System.Drawing.Size(54, 23);
        label1.TabIndex = 4;
        label1.Text = "開始/桁";
        // 
        // digitNumericUpDown1
        // 
        digitNumericUpDown1.Location = new System.Drawing.Point(1588, 94);
        digitNumericUpDown1.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        digitNumericUpDown1.Name = "digitNumericUpDown1";
        digitNumericUpDown1.Size = new System.Drawing.Size(60, 23);
        digitNumericUpDown1.TabIndex = 5;
        digitNumericUpDown1.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // startNumNumericUpDown
        // 
        startNumNumericUpDown.Location = new System.Drawing.Point(1462, 93);
        startNumNumericUpDown.Maximum = new decimal(new int[] { 10000000, 0, 0, 0 });
        startNumNumericUpDown.Name = "startNumNumericUpDown";
        startNumNumericUpDown.Size = new System.Drawing.Size(120, 23);
        startNumNumericUpDown.TabIndex = 6;
        // 
        // defaultAllSelectCheckBox
        // 
        defaultAllSelectCheckBox.Checked = true;
        defaultAllSelectCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
        defaultAllSelectCheckBox.Location = new System.Drawing.Point(1478, 377);
        defaultAllSelectCheckBox.Name = "defaultAllSelectCheckBox";
        defaultAllSelectCheckBox.Size = new System.Drawing.Size(127, 24);
        defaultAllSelectCheckBox.TabIndex = 8;
        defaultAllSelectCheckBox.Text = "デフォルトで全選択";
        defaultAllSelectCheckBox.UseVisualStyleBackColor = true;
        // 
        // renameButton
        // 
        renameButton.ForeColor = System.Drawing.Color.Red;
        renameButton.Location = new System.Drawing.Point(1412, 430);
        renameButton.Name = "renameButton";
        renameButton.Size = new System.Drawing.Size(250, 50);
        renameButton.TabIndex = 9;
        renameButton.Text = "リネーム";
        renameButton.UseVisualStyleBackColor = true;
        // 
        // allSelectButton
        // 
        allSelectButton.ForeColor = System.Drawing.Color.Red;
        allSelectButton.Location = new System.Drawing.Point(1462, 12);
        allSelectButton.Name = "allSelectButton";
        allSelectButton.Size = new System.Drawing.Size(90, 30);
        allSelectButton.TabIndex = 10;
        allSelectButton.Text = "全選択";
        allSelectButton.UseVisualStyleBackColor = true;
        // 
        // Form1
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(1684, 661);
        Controls.Add(allSelectButton);
        Controls.Add(renameButton);
        Controls.Add(defaultAllSelectCheckBox);
        Controls.Add(startNumNumericUpDown);
        Controls.Add(digitNumericUpDown1);
        Controls.Add(label1);
        Controls.Add(renameSettingLabel);
        Controls.Add(fileListView);
        Controls.Add(treeView1);
        Icon = ((System.Drawing.Icon)resources.GetObject("$this.Icon"));
        Text = "MhRenamer";
        ((System.ComponentModel.ISupportInitialize)digitNumericUpDown1).EndInit();
        ((System.ComponentModel.ISupportInitialize)startNumNumericUpDown).EndInit();
        ResumeLayout(false);
    }

    private System.Windows.Forms.Button allSelectButton;

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.NumericUpDown digitNumericUpDown1;
    private System.Windows.Forms.NumericUpDown startNumNumericUpDown;
    private System.Windows.Forms.CheckBox defaultAllSelectCheckBox;
    private System.Windows.Forms.Button renameButton;

    private System.Windows.Forms.Label renameSettingLabel;

    private System.Windows.Forms.ListView fileListView;

    private System.Windows.Forms.TreeView treeView1;

    #endregion
}
