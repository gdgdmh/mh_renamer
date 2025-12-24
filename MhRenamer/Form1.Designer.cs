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
        treeView1 = new System.Windows.Forms.TreeView();
        fileListView = new System.Windows.Forms.ListView();
        SuspendLayout();
        // 
        // treeView1
        // 
        treeView1.Location = new System.Drawing.Point(12, 12);
        treeView1.Name = "treeView1";
        treeView1.Size = new System.Drawing.Size(253, 286);
        treeView1.TabIndex = 0;
        // 
        // fileListView
        // 
        fileListView.Location = new System.Drawing.Point(291, 12);
        fileListView.Name = "fileListView";
        fileListView.Size = new System.Drawing.Size(478, 384);
        fileListView.TabIndex = 1;
        fileListView.UseCompatibleStateImageBehavior = false;
        // 
        // Form1
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(950, 450);
        Controls.Add(fileListView);
        Controls.Add(treeView1);
        Text = "Form1";
        ResumeLayout(false);
    }

    private System.Windows.Forms.ListView fileListView;

    private System.Windows.Forms.TreeView treeView1;

    #endregion
}