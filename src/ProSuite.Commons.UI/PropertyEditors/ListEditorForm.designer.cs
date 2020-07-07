using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.Commons.UI.PropertyEditors
{
    partial class ListEditorForm
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._propertyGrid = new System.Windows.Forms.PropertyGrid();
            this._dataGridView = new DoubleBufferedDataGridView();
            this.columnPosition = new System.Windows.Forms.DataGridViewButtonColumn();
            this.columnItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._buttonAdd = new System.Windows.Forms.Button();
            this._buttonRemove = new System.Windows.Forms.Button();
            this._buttonUp = new System.Windows.Forms.Button();
            this._buttonDown = new System.Windows.Forms.Button();
            this._buttonCancel = new System.Windows.Forms.Button();
            this._buttonOK = new System.Windows.Forms.Button();
            this.labelItem = new System.Windows.Forms.Label();
            this.labelDescription = new System.Windows.Forms.Label();
            this._textBoxItem = new System.Windows.Forms.TextBox();
            this._textBoxDescription = new System.Windows.Forms.TextBox();
            this._splitContainer = new SplitContainerEx();
            ((System.ComponentModel.ISupportInitialize)(this._dataGridView)).BeginInit();
	        ((System.ComponentModel.ISupportInitialize)(this._splitContainer)).BeginInit();
            this._splitContainer.Panel1.SuspendLayout();
            this._splitContainer.Panel2.SuspendLayout();
            this._splitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // _propertyGrid
            // 
            this._propertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._propertyGrid.Location = new System.Drawing.Point(3, 0);
            this._propertyGrid.Name = "_propertyGrid";
            this._propertyGrid.PropertySort = System.Windows.Forms.PropertySort.NoSort;
            this._propertyGrid.Size = new System.Drawing.Size(274, 282);
            this._propertyGrid.TabIndex = 0;
            this._propertyGrid.SelectedGridItemChanged += new System.Windows.Forms.SelectedGridItemChangedEventHandler(this._propertyGrid_SelectedGridItemChanged);
            this._propertyGrid.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this._propertyGrid_PropertyValueChanged);
            // 
            // _dataGridView
            // 
            this._dataGridView.AllowUserToAddRows = false;
            this._dataGridView.AllowUserToDeleteRows = false;
            this._dataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this._dataGridView.ColumnHeadersVisible = false;
            this._dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.columnPosition,
            this.columnItem});
            this._dataGridView.Location = new System.Drawing.Point(0, 0);
            this._dataGridView.Name = "_dataGridView";
            this._dataGridView.ReadOnly = true;
            this._dataGridView.RowHeadersVisible = false;
            this._dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._dataGridView.Size = new System.Drawing.Size(195, 253);
            this._dataGridView.TabIndex = 1;
            this._dataGridView.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this._dataGridView_RowsAdded);
            this._dataGridView.RowsRemoved += new System.Windows.Forms.DataGridViewRowsRemovedEventHandler(this._dataGridView_RowsRemoved);
            this._dataGridView.SelectionChanged += new System.EventHandler(this._dataGridView_SelectionChanged);
            // 
            // columnPosition
            // 
            this.columnPosition.HeaderText = "Position";
            this.columnPosition.MinimumWidth = 24;
            this.columnPosition.Name = "columnPosition";
            this.columnPosition.ReadOnly = true;
            this.columnPosition.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.columnPosition.Width = 24;
            // 
            // columnItem
            // 
            this.columnItem.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.columnItem.HeaderText = "Item";
            this.columnItem.MinimumWidth = 20;
            this.columnItem.Name = "columnItem";
            this.columnItem.ReadOnly = true;
            // 
            // _buttonAdd
            // 
            this._buttonAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._buttonAdd.Location = new System.Drawing.Point(-1, 259);
            this._buttonAdd.Name = "_buttonAdd";
            this._buttonAdd.Size = new System.Drawing.Size(64, 23);
            this._buttonAdd.TabIndex = 2;
            this._buttonAdd.Text = "Add";
            this._buttonAdd.UseVisualStyleBackColor = true;
            this._buttonAdd.Click += new System.EventHandler(this._buttonAdd_Click);
            // 
            // _buttonRemove
            // 
            this._buttonRemove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._buttonRemove.Location = new System.Drawing.Point(69, 259);
            this._buttonRemove.Name = "_buttonRemove";
            this._buttonRemove.Size = new System.Drawing.Size(64, 23);
            this._buttonRemove.TabIndex = 3;
            this._buttonRemove.Text = "Remove";
            this._buttonRemove.UseVisualStyleBackColor = true;
            this._buttonRemove.Click += new System.EventHandler(this._buttonRemove_Click);
            // 
            // _buttonUp
            // 
            this._buttonUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonUp.Image = global::ProSuite.Commons.UI.Properties.Resources.MoveUp;
            this._buttonUp.Location = new System.Drawing.Point(201, 0);
            this._buttonUp.Name = "_buttonUp";
            this._buttonUp.Size = new System.Drawing.Size(28, 28);
            this._buttonUp.TabIndex = 4;
            this._buttonUp.UseVisualStyleBackColor = true;
            this._buttonUp.Click += new System.EventHandler(this._buttonUp_Click);
            // 
            // _buttonDown
            // 
            this._buttonDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonDown.Image = global::ProSuite.Commons.UI.Properties.Resources.MoveDown;
            this._buttonDown.Location = new System.Drawing.Point(202, 32);
            this._buttonDown.Name = "_buttonDown";
            this._buttonDown.Size = new System.Drawing.Size(28, 28);
            this._buttonDown.TabIndex = 5;
            this._buttonDown.UseVisualStyleBackColor = true;
            this._buttonDown.Click += new System.EventHandler(this._buttonDown_Click);
            // 
            // _buttonCancel
            // 
            this._buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._buttonCancel.Location = new System.Drawing.Point(453, 387);
            this._buttonCancel.Name = "_buttonCancel";
            this._buttonCancel.Size = new System.Drawing.Size(75, 23);
            this._buttonCancel.TabIndex = 7;
            this._buttonCancel.Text = "Cancel";
            this._buttonCancel.UseVisualStyleBackColor = true;
            this._buttonCancel.Click += new System.EventHandler(this._buttonCancel_Click);
            // 
            // _buttonOK
            // 
            this._buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonOK.Location = new System.Drawing.Point(372, 387);
            this._buttonOK.Name = "_buttonOK";
            this._buttonOK.Size = new System.Drawing.Size(75, 23);
            this._buttonOK.TabIndex = 6;
            this._buttonOK.Text = "OK";
            this._buttonOK.UseVisualStyleBackColor = true;
            this._buttonOK.Click += new System.EventHandler(this._buttonOK_Click);
            // 
            // labelItem
            // 
            this.labelItem.AutoSize = true;
            this.labelItem.Location = new System.Drawing.Point(13, 13);
            this.labelItem.Name = "labelItem";
            this.labelItem.Size = new System.Drawing.Size(49, 13);
            this.labelItem.TabIndex = 8;
            this.labelItem.Text = "Property:";
            // 
            // labelDescription
            // 
            this.labelDescription.AutoSize = true;
            this.labelDescription.Location = new System.Drawing.Point(12, 39);
            this.labelDescription.Name = "labelDescription";
            this.labelDescription.Size = new System.Drawing.Size(63, 13);
            this.labelDescription.TabIndex = 9;
            this.labelDescription.Text = "Description:";
            // 
            // _textBoxItem
            // 
            this._textBoxItem.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._textBoxItem.Location = new System.Drawing.Point(79, 10);
            this._textBoxItem.Name = "_textBoxItem";
            this._textBoxItem.ReadOnly = true;
            this._textBoxItem.Size = new System.Drawing.Size(449, 20);
            this._textBoxItem.TabIndex = 10;
            // 
            // _textBoxDescription
            // 
            this._textBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._textBoxDescription.Location = new System.Drawing.Point(79, 36);
            this._textBoxDescription.Multiline = true;
            this._textBoxDescription.Name = "_textBoxDescription";
            this._textBoxDescription.ReadOnly = true;
            this._textBoxDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._textBoxDescription.Size = new System.Drawing.Size(449, 47);
            this._textBoxDescription.TabIndex = 11;
            // 
            // _splitContainer
            // 
            this._splitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._splitContainer.Location = new System.Drawing.Point(16, 99);
            this._splitContainer.Name = "_splitContainer";
            // 
            // _splitContainer.Panel1
            // 
            this._splitContainer.Panel1.Controls.Add(this._dataGridView);
            this._splitContainer.Panel1.Controls.Add(this._buttonAdd);
            this._splitContainer.Panel1.Controls.Add(this._buttonRemove);
            this._splitContainer.Panel1.Controls.Add(this._buttonUp);
            this._splitContainer.Panel1.Controls.Add(this._buttonDown);
            // 
            // _splitContainer.Panel2
            // 
            this._splitContainer.Panel2.Controls.Add(this._propertyGrid);
            this._splitContainer.Size = new System.Drawing.Size(512, 282);
            this._splitContainer.SplitterDistance = 231;
            this._splitContainer.TabIndex = 12;
            // 
            // ListEditorForm
            // 
            this.AcceptButton = this._buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._buttonCancel;
            this.ClientSize = new System.Drawing.Size(540, 422);
            this.Controls.Add(this._splitContainer);
            this.Controls.Add(this._textBoxDescription);
            this.Controls.Add(this._textBoxItem);
            this.Controls.Add(this.labelDescription);
            this.Controls.Add(this.labelItem);
            this.Controls.Add(this._buttonCancel);
            this.Controls.Add(this._buttonOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ListEditorForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "List Editor";
            ((System.ComponentModel.ISupportInitialize)(this._dataGridView)).EndInit();
            this._splitContainer.Panel1.ResumeLayout(false);
            this._splitContainer.Panel2.ResumeLayout(false);
	        ((System.ComponentModel.ISupportInitialize)(this._splitContainer)).EndInit();
            this._splitContainer.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PropertyGrid _propertyGrid;
        private System.Windows.Forms.Button _buttonAdd;
        private System.Windows.Forms.Button _buttonRemove;
        private System.Windows.Forms.Button _buttonUp;
        private System.Windows.Forms.Button _buttonDown;
        private System.Windows.Forms.Button _buttonCancel;
        private System.Windows.Forms.Button _buttonOK;
        private System.Windows.Forms.DataGridViewButtonColumn columnPosition;
        private System.Windows.Forms.DataGridViewTextBoxColumn columnItem;
        private System.Windows.Forms.Label labelItem;
        private System.Windows.Forms.Label labelDescription;
        private System.Windows.Forms.TextBox _textBoxItem;
        private System.Windows.Forms.TextBox _textBoxDescription;
        private SplitContainerEx _splitContainer;
        private DoubleBufferedDataGridView _dataGridView;
    }
}
