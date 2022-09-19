using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ProSuite.DdxEditor.Framework.NavigationPanel
{
    partial class NavigationControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new Container();
            this._treeView = new TreeView();
            this._imageList = new ImageList(this.components);
            this._contextMenuStrip = new ContextMenuStrip(this.components);
            this.SuspendLayout();
            // 
            // _treeView
            // 
            this._treeView.BorderStyle = BorderStyle.None;
            this._treeView.Dock = DockStyle.Fill;
            this._treeView.FullRowSelect = true;
            this._treeView.HideSelection = false;
            this._treeView.ImageIndex = 0;
            this._treeView.ImageList = this._imageList;
            this._treeView.Location = new Point(0, 0);
            this._treeView.Name = "_treeView";
            this._treeView.SelectedImageIndex = 0;
            this._treeView.Size = new Size(150, 150);
            this._treeView.TabIndex = 0;
            this._treeView.BeforeExpand += new TreeViewCancelEventHandler(this._treeView_BeforeExpand);
            this._treeView.AfterSelect += new TreeViewEventHandler(this._treeView_AfterSelect);
            this._treeView.NodeMouseClick += new TreeNodeMouseClickEventHandler(this._treeView_NodeMouseClick);
            this._treeView.BeforeSelect += new TreeViewCancelEventHandler(this._treeView_BeforeSelect);
            this._treeView.AfterExpand += new TreeViewEventHandler(this._treeView_AfterExpand);
            // 
            // _imageList
            // 
            this._imageList.ColorDepth = ColorDepth.Depth16Bit;
            this._imageList.ImageSize = new Size(16, 16);
            this._imageList.TransparentColor = Color.Transparent;
            // 
            // _contextMenuStrip
            // 
            this._contextMenuStrip.Name = "_contextMenuStrip";
            this._contextMenuStrip.Size = new Size(153, 26);
            this._contextMenuStrip.Closed += new ToolStripDropDownClosedEventHandler(this._contextMenuStrip_Closed);
            // 
            // NavigationControl
            // 
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Controls.Add(this._treeView);
            this.Name = "NavigationControl";
            this.ResumeLayout(false);

        }

        #endregion

        private TreeView _treeView;
        private ContextMenuStrip _contextMenuStrip;
        private ImageList _imageList;
    }
}