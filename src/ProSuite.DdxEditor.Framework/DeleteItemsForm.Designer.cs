namespace ProSuite.DdxEditor.Framework
{
	partial class DeleteItemsForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this._buttonNoClose = new System.Windows.Forms.Button();
			this._buttonYes = new System.Windows.Forms.Button();
			this._splitContainer = new ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx();
			this._treeView = new System.Windows.Forms.TreeView();
			this._imageList = new System.Windows.Forms.ImageList(this.components);
			this._dataGridView = new System.Windows.Forms.DataGridView();
			this._columnImage = new System.Windows.Forms.DataGridViewImageColumn();
			this._columnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnTypeName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnAction = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._labelFooter = new System.Windows.Forms.Label();
			this._labelHeader = new System.Windows.Forms.Label();
			this._pictureBoxImage = new System.Windows.Forms.PictureBox();
			this._columnDependingItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnRequiresConfirmation = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnCanRemove = new System.Windows.Forms.DataGridViewTextBoxColumn();
			((System.ComponentModel.ISupportInitialize)(this._splitContainer)).BeginInit();
			this._splitContainer.Panel1.SuspendLayout();
			this._splitContainer.Panel2.SuspendLayout();
			this._splitContainer.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridView)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this._pictureBoxImage)).BeginInit();
			this.SuspendLayout();
			// 
			// _buttonNoClose
			// 
			this._buttonNoClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonNoClose.DialogResult = System.Windows.Forms.DialogResult.No;
			this._buttonNoClose.Location = new System.Drawing.Point(834, 492);
			this._buttonNoClose.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
			this._buttonNoClose.Name = "_buttonNoClose";
			this._buttonNoClose.Size = new System.Drawing.Size(125, 44);
			this._buttonNoClose.TabIndex = 1;
			this._buttonNoClose.Text = "<NoClose>";
			this._buttonNoClose.UseVisualStyleBackColor = true;
			this._buttonNoClose.Click += new System.EventHandler(this._buttonNo_Click);
			// 
			// _buttonYes
			// 
			this._buttonYes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonYes.Location = new System.Drawing.Point(699, 492);
			this._buttonYes.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
			this._buttonYes.Name = "_buttonYes";
			this._buttonYes.Size = new System.Drawing.Size(125, 44);
			this._buttonYes.TabIndex = 0;
			this._buttonYes.Text = "Yes";
			this._buttonYes.UseVisualStyleBackColor = true;
			this._buttonYes.Click += new System.EventHandler(this._buttonYes_Click);
			// 
			// _splitContainer
			// 
			this._splitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._splitContainer.Location = new System.Drawing.Point(20, 90);
			this._splitContainer.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
			this._splitContainer.Name = "_splitContainer";
			// 
			// _splitContainer.Panel1
			// 
			this._splitContainer.Panel1.Controls.Add(this._treeView);
			this._splitContainer.Panel1MinSize = 150;
			// 
			// _splitContainer.Panel2
			// 
			this._splitContainer.Panel2.Controls.Add(this._dataGridView);
			this._splitContainer.Panel2MinSize = 50;
			this._splitContainer.Size = new System.Drawing.Size(939, 382);
			this._splitContainer.SplitterDistance = 367;
			this._splitContainer.SplitterWidth = 7;
			this._splitContainer.TabIndex = 2;
			this._splitContainer.TabStop = false;
			// 
			// _treeView
			// 
			this._treeView.Dock = System.Windows.Forms.DockStyle.Fill;
			this._treeView.FullRowSelect = true;
			this._treeView.HideSelection = false;
			this._treeView.ImageIndex = 0;
			this._treeView.ImageList = this._imageList;
			this._treeView.Location = new System.Drawing.Point(0, 0);
			this._treeView.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
			this._treeView.Name = "_treeView";
			this._treeView.SelectedImageIndex = 0;
			this._treeView.Size = new System.Drawing.Size(367, 382);
			this._treeView.TabIndex = 0;
			this._treeView.BeforeSelect += new System.Windows.Forms.TreeViewCancelEventHandler(this._treeView_BeforeSelect);
			// 
			// _imageList
			// 
			this._imageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth16Bit;
			this._imageList.ImageSize = new System.Drawing.Size(16, 16);
			this._imageList.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// _dataGridView
			// 
			this._dataGridView.AllowUserToAddRows = false;
			this._dataGridView.AllowUserToDeleteRows = false;
			this._dataGridView.AllowUserToResizeRows = false;
			this._dataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this._dataGridView.ColumnHeadersHeight = 34;
			this._dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			this._dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._columnImage,
            this._columnName,
            this._columnTypeName,
            this._columnAction});
			this._dataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this._dataGridView.Location = new System.Drawing.Point(0, 0);
			this._dataGridView.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
			this._dataGridView.Name = "_dataGridView";
			this._dataGridView.ReadOnly = true;
			this._dataGridView.RowHeadersVisible = false;
			this._dataGridView.RowHeadersWidth = 62;
			this._dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridView.Size = new System.Drawing.Size(565, 382);
			this._dataGridView.StandardTab = true;
			this._dataGridView.TabIndex = 0;
			this._dataGridView.SelectionChanged += new System.EventHandler(this._dataGridView_SelectionChanged);
			// 
			// _columnImage
			// 
			this._columnImage.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnImage.DataPropertyName = "Image";
			this._columnImage.HeaderText = "";
			this._columnImage.MinimumWidth = 8;
			this._columnImage.Name = "_columnImage";
			this._columnImage.ReadOnly = true;
			this._columnImage.Width = 8;
			// 
			// _columnName
			// 
			this._columnName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnName.DataPropertyName = "Name";
			this._columnName.HeaderText = "Depending Item";
			this._columnName.MinimumWidth = 8;
			this._columnName.Name = "_columnName";
			this._columnName.ReadOnly = true;
			this._columnName.Width = 177;
			// 
			// _columnTypeName
			// 
			this._columnTypeName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnTypeName.DataPropertyName = "Type";
			this._columnTypeName.HeaderText = "Item Type";
			this._columnTypeName.MinimumWidth = 8;
			this._columnTypeName.Name = "_columnTypeName";
			this._columnTypeName.ReadOnly = true;
			this._columnTypeName.Width = 126;
			// 
			// _columnAction
			// 
			this._columnAction.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._columnAction.DataPropertyName = "Action";
			this._columnAction.HeaderText = "Action";
			this._columnAction.MinimumWidth = 8;
			this._columnAction.Name = "_columnAction";
			this._columnAction.ReadOnly = true;
			// 
			// _labelFooter
			// 
			this._labelFooter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._labelFooter.AutoEllipsis = true;
			this._labelFooter.BackColor = System.Drawing.SystemColors.Control;
			this._labelFooter.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this._labelFooter.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this._labelFooter.Location = new System.Drawing.Point(20, 500);
			this._labelFooter.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
			this._labelFooter.Name = "_labelFooter";
			this._labelFooter.Size = new System.Drawing.Size(669, 35);
			this._labelFooter.TabIndex = 4;
			this._labelFooter.Text = "<footer>";
			this._labelFooter.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelHeader
			// 
			this._labelHeader.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._labelHeader.AutoEllipsis = true;
			this._labelHeader.AutoSize = true;
			this._labelHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this._labelHeader.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this._labelHeader.Location = new System.Drawing.Point(83, 35);
			this._labelHeader.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
			this._labelHeader.Name = "_labelHeader";
			this._labelHeader.Size = new System.Drawing.Size(90, 25);
			this._labelHeader.TabIndex = 0;
			this._labelHeader.Text = "<header>";
			// 
			// _pictureBoxImage
			// 
			this._pictureBoxImage.Location = new System.Drawing.Point(20, 17);
			this._pictureBoxImage.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
			this._pictureBoxImage.Name = "_pictureBoxImage";
			this._pictureBoxImage.Size = new System.Drawing.Size(53, 62);
			this._pictureBoxImage.TabIndex = 0;
			this._pictureBoxImage.TabStop = false;
			// 
			// _columnDependingItem
			// 
			this._columnDependingItem.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._columnDependingItem.DataPropertyName = "Name";
			this._columnDependingItem.FillWeight = 129.9492F;
			this._columnDependingItem.HeaderText = "Existing reference to depending item";
			this._columnDependingItem.MinimumWidth = 8;
			this._columnDependingItem.Name = "_columnDependingItem";
			this._columnDependingItem.ReadOnly = true;
			// 
			// _columnRequiresConfirmation
			// 
			this._columnRequiresConfirmation.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._columnRequiresConfirmation.DataPropertyName = "RequiresConfirmation";
			this._columnRequiresConfirmation.FillWeight = 129.9492F;
			this._columnRequiresConfirmation.HeaderText = "Requires confirmation";
			this._columnRequiresConfirmation.MinimumWidth = 8;
			this._columnRequiresConfirmation.Name = "_columnRequiresConfirmation";
			this._columnRequiresConfirmation.ReadOnly = true;
			// 
			// _columnCanRemove
			// 
			this._columnCanRemove.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._columnCanRemove.DataPropertyName = "CanRemove";
			this._columnCanRemove.FillWeight = 129.9492F;
			this._columnCanRemove.HeaderText = "Can remove";
			this._columnCanRemove.MinimumWidth = 8;
			this._columnCanRemove.Name = "_columnCanRemove";
			this._columnCanRemove.ReadOnly = true;
			// 
			// DeleteItemsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this._buttonNoClose;
			this.ClientSize = new System.Drawing.Size(985, 576);
			this.Controls.Add(this._buttonNoClose);
			this.Controls.Add(this._buttonYes);
			this.Controls.Add(this._labelFooter);
			this.Controls.Add(this._pictureBoxImage);
			this.Controls.Add(this._splitContainer);
			this.Controls.Add(this._labelHeader);
			this.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(985, 564);
			this.Name = "DeleteItemsForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.Text = "Delete";
			this.Load += new System.EventHandler(this.DeleteItemsForm_Load);
			this._splitContainer.Panel1.ResumeLayout(false);
			this._splitContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._splitContainer)).EndInit();
			this._splitContainer.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._dataGridView)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this._pictureBoxImage)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button _buttonNoClose;
		private System.Windows.Forms.Button _buttonYes;
		private global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx _splitContainer;
		private System.Windows.Forms.TreeView _treeView;
		private System.Windows.Forms.DataGridView _dataGridView;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnDependingItem;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnRequiresConfirmation;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnCanRemove;
		private System.Windows.Forms.ImageList _imageList;
		private System.Windows.Forms.Label _labelFooter;
		private System.Windows.Forms.Label _labelHeader;
		private System.Windows.Forms.PictureBox _pictureBoxImage;
		private System.Windows.Forms.DataGridViewImageColumn _columnImage;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnName;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnTypeName;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnAction;
	}
}