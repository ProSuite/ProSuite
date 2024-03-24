using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	partial class InstanceConfigurationControl
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
			components=new System.ComponentModel.Container();
			_textBoxName=new System.Windows.Forms.TextBox();
			_labelName=new System.Windows.Forms.Label();
			_textBoxDescription=new System.Windows.Forms.TextBox();
			_labelDescription=new System.Windows.Forms.Label();
			_errorProvider=new System.Windows.Forms.ErrorProvider(components);
			_labelInstanceDescriptor=new System.Windows.Forms.Label();
			_buttonGoToInstanceDescriptor = new System.Windows.Forms.Button();
			openFileDialogImport=new System.Windows.Forms.OpenFileDialog();
			saveFileDialogExport=new System.Windows.Forms.SaveFileDialog();
			_textBoxQualitySpecifications=new System.Windows.Forms.TextBox();
			_labelQualitySpecifications=new System.Windows.Forms.Label();
			_textBoxUrl=new System.Windows.Forms.TextBox();
			_labelUrl=new System.Windows.Forms.Label();
			_buttonOpenUrl=new System.Windows.Forms.Button();
			_textBoxCategory=new System.Windows.Forms.TextBox();
			_labelCategory=new System.Windows.Forms.Label();
			_toolTip=new System.Windows.Forms.ToolTip(components);
			_objectReferenceControlInstanceDescriptor=new ObjectReferenceControl();
			dataGridViewTextBoxColumn1=new System.Windows.Forms.DataGridViewTextBoxColumn();
			_tabPageNotes=new System.Windows.Forms.TabPage();
			_textBoxNotes=new System.Windows.Forms.TextBox();
			_tabPageReferencing=new System.Windows.Forms.TabPage();
			_dataGridViewReferences=new DoubleBufferedDataGridView();
			_columnImage=new System.Windows.Forms.DataGridViewImageColumn();
			_columnName=new System.Windows.Forms.DataGridViewTextBoxColumn();
			_columnType=new System.Windows.Forms.DataGridViewTextBoxColumn();
			_columnAlgorithm=new System.Windows.Forms.DataGridViewTextBoxColumn();
			_columnDescription=new System.Windows.Forms.DataGridViewTextBoxColumn();
			_columnCategory=new System.Windows.Forms.DataGridViewTextBoxColumn();
			_toolStripElements=new ToolStripEx();
			toolStripLabel1=new System.Windows.Forms.ToolStripLabel();
			miniToolStrip=new ToolStripEx();
			_tabPageParameters=new System.Windows.Forms.TabPage();
			_instanceConfigTableViewControlPanel=new System.Windows.Forms.Panel();
			_splitContainer=new SplitContainerEx();
			_panelParamsDesc=new System.Windows.Forms.Panel();
			_textBoxDescGrid=new System.Windows.Forms.TextBox();
			labelDescGrid=new System.Windows.Forms.Label();
			_panelParametersEdit=new System.Windows.Forms.Panel();
			_panelParametersTop=new System.Windows.Forms.Panel();
			_linkDocumentation=new System.Windows.Forms.LinkLabel();
			_splitContainerHeader=new SplitContainerEx();
			_splitContainerProperties=new SplitContainerEx();
			_textBoxDescProps=new System.Windows.Forms.TextBox();
			_panelDescriptionLabel=new System.Windows.Forms.Panel();
			_labelParameterDescription=new System.Windows.Forms.Label();
			_tabControlDetails=new System.Windows.Forms.TabControl();
			((System.ComponentModel.ISupportInitialize)_errorProvider).BeginInit();
			_tabPageNotes.SuspendLayout();
			_tabPageReferencing.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)_dataGridViewReferences).BeginInit();
			_toolStripElements.SuspendLayout();
			_tabPageParameters.SuspendLayout();
			_instanceConfigTableViewControlPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)_splitContainer).BeginInit();
			_splitContainer.Panel1.SuspendLayout();
			_splitContainer.Panel2.SuspendLayout();
			_splitContainer.SuspendLayout();
			_panelParamsDesc.SuspendLayout();
			_panelParametersTop.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)_splitContainerHeader).BeginInit();
			_splitContainerHeader.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)_splitContainerProperties).BeginInit();
			_splitContainerProperties.Panel1.SuspendLayout();
			_splitContainerProperties.SuspendLayout();
			_panelDescriptionLabel.SuspendLayout();
			_tabControlDetails.SuspendLayout();
			SuspendLayout();
			// 
			// _textBoxName
			// 
			_textBoxName.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_textBoxName.Location=new System.Drawing.Point(109, 15);
			_textBoxName.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_textBoxName.Name="_textBoxName";
			_textBoxName.Size=new System.Drawing.Size(256, 23);
			_textBoxName.TabIndex=0;
			_toolTip.SetToolTip(_textBoxName, "Press TAB to suggest a name (a feature class / table must be configured)");
			_textBoxName.PreviewKeyDown+=_textBoxName_PreviewKeyDown;
			// 
			// _labelName
			// 
			_labelName.AutoSize=true;
			_labelName.Location=new System.Drawing.Point(59, 18);
			_labelName.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelName.Name="_labelName";
			_labelName.Size=new System.Drawing.Size(42, 15);
			_labelName.TabIndex=1;
			_labelName.Text="Name:";
			_labelName.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxDescription
			// 
			_textBoxDescription.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_textBoxDescription.Location=new System.Drawing.Point(109, 46);
			_textBoxDescription.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_textBoxDescription.Multiline=true;
			_textBoxDescription.Name="_textBoxDescription";
			_textBoxDescription.ScrollBars=System.Windows.Forms.ScrollBars.Vertical;
			_textBoxDescription.Size=new System.Drawing.Size(592, 56);
			_textBoxDescription.TabIndex=2;
			// 
			// _labelDescription
			// 
			_labelDescription.AutoSize=true;
			_labelDescription.Location=new System.Drawing.Point(30, 50);
			_labelDescription.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelDescription.Name="_labelDescription";
			_labelDescription.Size=new System.Drawing.Size(70, 15);
			_labelDescription.TabIndex=3;
			_labelDescription.Text="Description:";
			_labelDescription.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _errorProvider
			// 
			_errorProvider.BlinkStyle=System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
			_errorProvider.ContainerControl=this;
			// 
			// _labelInstanceDescriptor
			// 
			_labelInstanceDescriptor.AutoSize=true;
			_labelInstanceDescriptor.Location=new System.Drawing.Point(4, 142);
			_labelInstanceDescriptor.Margin=new System.Windows.Forms.Padding(6, 0, 6, 0);
			_labelInstanceDescriptor.Name="_labelInstanceDescriptor";
			_labelInstanceDescriptor.Size=new System.Drawing.Size(95, 15);
			_labelInstanceDescriptor.TabIndex=9;
			_labelInstanceDescriptor.Text="Implementation:";
			_labelInstanceDescriptor.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _buttonGoToInstanceDescriptor
			// 
			_buttonGoToInstanceDescriptor.FlatAppearance.BorderSize = 0;
			_buttonGoToInstanceDescriptor.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			_buttonGoToInstanceDescriptor.Image = global::ProSuite.DdxEditor.Framework.Properties.Resources.GoToItem;
			_buttonGoToInstanceDescriptor.Location = new System.Drawing.Point(109, 140);
			_buttonGoToInstanceDescriptor.Margin = new System.Windows.Forms.Padding(1);
			_buttonGoToInstanceDescriptor.Name = "_buttonGoToInstanceDescriptor";
			_buttonGoToInstanceDescriptor.Size = new System.Drawing.Size(18, 22);
			_buttonGoToInstanceDescriptor.TabIndex = 40;
			_buttonGoToInstanceDescriptor.UseVisualStyleBackColor = true;
			_buttonGoToInstanceDescriptor.Click += _buttonGoToInstanceDescriptor_Clicked;
			// 
			// _textBoxQualitySpecifications
			// 
			_textBoxQualitySpecifications.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_textBoxQualitySpecifications.Location=new System.Drawing.Point(109, 171);
			_textBoxQualitySpecifications.Margin=new System.Windows.Forms.Padding(6, 5, 6, 5);
			_textBoxQualitySpecifications.Name="_textBoxQualitySpecifications";
			_textBoxQualitySpecifications.ReadOnly=true;
			_textBoxQualitySpecifications.Size=new System.Drawing.Size(592, 23);
			_textBoxQualitySpecifications.TabIndex=8;
			_textBoxQualitySpecifications.TabStop=false;
			// 
			// _labelQualitySpecifications
			// 
			_labelQualitySpecifications.AutoSize=true;
			_labelQualitySpecifications.Location=new System.Drawing.Point(50, 174);
			_labelQualitySpecifications.Margin=new System.Windows.Forms.Padding(6, 0, 6, 0);
			_labelQualitySpecifications.Name="_labelQualitySpecifications";
			_labelQualitySpecifications.Size=new System.Drawing.Size(49, 15);
			_labelQualitySpecifications.TabIndex=32;
			_labelQualitySpecifications.Text="Used in:";
			_labelQualitySpecifications.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxUrl
			// 
			_textBoxUrl.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_textBoxUrl.Location=new System.Drawing.Point(109, 110);
			_textBoxUrl.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_textBoxUrl.Name="_textBoxUrl";
			_textBoxUrl.Size=new System.Drawing.Size(555, 23);
			_textBoxUrl.TabIndex=3;
			// 
			// _labelUrl
			// 
			_labelUrl.AutoSize=true;
			_labelUrl.Location=new System.Drawing.Point(68, 114);
			_labelUrl.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelUrl.Name="_labelUrl";
			_labelUrl.Size=new System.Drawing.Size(31, 15);
			_labelUrl.TabIndex=1;
			_labelUrl.Text="URL:";
			_labelUrl.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _buttonOpenUrl
			// 
			_buttonOpenUrl.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Right;
			_buttonOpenUrl.FlatAppearance.BorderSize=0;
			_buttonOpenUrl.FlatStyle=System.Windows.Forms.FlatStyle.Flat;
			_buttonOpenUrl.Image=Properties.Resources.OpenUrl;
			_buttonOpenUrl.Location=new System.Drawing.Point(671, 106);
			_buttonOpenUrl.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_buttonOpenUrl.Name="_buttonOpenUrl";
			_buttonOpenUrl.Size=new System.Drawing.Size(30, 30);
			_buttonOpenUrl.TabIndex=4;
			_buttonOpenUrl.UseVisualStyleBackColor=true;
			_buttonOpenUrl.Click+=_buttonOpenUrl_Click;
			// 
			// _textBoxCategory
			// 
			_textBoxCategory.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Right;
			_textBoxCategory.Location=new System.Drawing.Point(466, 15);
			_textBoxCategory.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_textBoxCategory.Name="_textBoxCategory";
			_textBoxCategory.ReadOnly=true;
			_textBoxCategory.Size=new System.Drawing.Size(234, 23);
			_textBoxCategory.TabIndex=1;
			_textBoxCategory.TabStop=false;
			// 
			// _labelCategory
			// 
			_labelCategory.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Right;
			_labelCategory.AutoSize=true;
			_labelCategory.Location=new System.Drawing.Point(398, 18);
			_labelCategory.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelCategory.Name="_labelCategory";
			_labelCategory.Size=new System.Drawing.Size(58, 15);
			_labelCategory.TabIndex=3;
			_labelCategory.Text="Category:";
			_labelCategory.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _toolTip
			// 
			_toolTip.AutomaticDelay=100;
			_toolTip.AutoPopDelay=5000;
			_toolTip.InitialDelay=100;
			_toolTip.IsBalloon=true;
			_toolTip.ReshowDelay=20;
			_toolTip.ShowAlways=true;
			// 
			// _objectReferenceControlInstanceDescriptor
			// 
			_objectReferenceControlInstanceDescriptor.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_objectReferenceControlInstanceDescriptor.DataSource=null;
			_objectReferenceControlInstanceDescriptor.DisplayMember=null;
			_objectReferenceControlInstanceDescriptor.FindObjectDelegate=null;
			_objectReferenceControlInstanceDescriptor.Font=new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			_objectReferenceControlInstanceDescriptor.FormatTextDelegate=null;
			_objectReferenceControlInstanceDescriptor.Location=new System.Drawing.Point(133, 142);
			_objectReferenceControlInstanceDescriptor.Margin=new System.Windows.Forms.Padding(6, 5, 6, 5);
			_objectReferenceControlInstanceDescriptor.Name="_objectReferenceControlInstanceDescriptor";
			_objectReferenceControlInstanceDescriptor.ReadOnly=false;
			_objectReferenceControlInstanceDescriptor.Size=new System.Drawing.Size(568, 23);
			_objectReferenceControlInstanceDescriptor.TabIndex=10;
			// 
			// dataGridViewTextBoxColumn1
			// 
			dataGridViewTextBoxColumn1.MinimumWidth=8;
			dataGridViewTextBoxColumn1.Name="dataGridViewTextBoxColumn1";
			dataGridViewTextBoxColumn1.Width=150;
			// 
			// _tabPageNotes
			// 
			_tabPageNotes.Controls.Add(_textBoxNotes);
			_tabPageNotes.Location=new System.Drawing.Point(4, 24);
			_tabPageNotes.Margin=new System.Windows.Forms.Padding(6, 5, 6, 5);
			_tabPageNotes.Name="_tabPageNotes";
			_tabPageNotes.Padding=new System.Windows.Forms.Padding(10, 12, 10, 12);
			_tabPageNotes.Size=new System.Drawing.Size(693, 322);
			_tabPageNotes.TabIndex=3;
			_tabPageNotes.Text="Notes";
			_tabPageNotes.UseVisualStyleBackColor=true;
			// 
			// _textBoxNotes
			// 
			_textBoxNotes.Dock=System.Windows.Forms.DockStyle.Fill;
			_textBoxNotes.Location=new System.Drawing.Point(10, 12);
			_textBoxNotes.Margin=new System.Windows.Forms.Padding(6, 5, 6, 5);
			_textBoxNotes.Multiline=true;
			_textBoxNotes.Name="_textBoxNotes";
			_textBoxNotes.ScrollBars=System.Windows.Forms.ScrollBars.Vertical;
			_textBoxNotes.Size=new System.Drawing.Size(673, 298);
			_textBoxNotes.TabIndex=0;
			// 
			// _tabPageReferencing
			// 
			_tabPageReferencing.Controls.Add(_dataGridViewReferences);
			_tabPageReferencing.Controls.Add(_toolStripElements);
			_tabPageReferencing.Location=new System.Drawing.Point(4, 24);
			_tabPageReferencing.Margin=new System.Windows.Forms.Padding(6, 5, 6, 5);
			_tabPageReferencing.Name="_tabPageReferencing";
			_tabPageReferencing.Padding=new System.Windows.Forms.Padding(6, 5, 6, 5);
			_tabPageReferencing.Size=new System.Drawing.Size(693, 322);
			_tabPageReferencing.TabIndex=1;
			_tabPageReferencing.Text="Usage";
			_tabPageReferencing.UseVisualStyleBackColor=true;
			// 
			// _dataGridViewReferences
			// 
			_dataGridViewReferences.AllowUserToAddRows=false;
			_dataGridViewReferences.AllowUserToDeleteRows=false;
			_dataGridViewReferences.AllowUserToResizeRows=false;
			_dataGridViewReferences.ColumnHeadersHeightSizeMode=System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			_dataGridViewReferences.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { _columnImage, _columnName, _columnType, _columnAlgorithm, _columnDescription, _columnCategory });
			_dataGridViewReferences.Dock=System.Windows.Forms.DockStyle.Fill;
			_dataGridViewReferences.EditMode=System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
			_dataGridViewReferences.Location=new System.Drawing.Point(6, 53);
			_dataGridViewReferences.Margin=new System.Windows.Forms.Padding(6, 5, 6, 5);
			_dataGridViewReferences.Name="_dataGridViewReferences";
			_dataGridViewReferences.RowHeadersVisible=false;
			_dataGridViewReferences.RowHeadersWidth=62;
			_dataGridViewReferences.SelectionMode=System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			_dataGridViewReferences.Size=new System.Drawing.Size(681, 264);
			_dataGridViewReferences.TabIndex=24;
			_dataGridViewReferences.CellDoubleClick+=_dataGridViewQualitySpecifications_CellDoubleClick;
			_dataGridViewReferences.CellEndEdit+=_dataGridViewQualitySpecifications_CellEndEdit;
			_dataGridViewReferences.CellValueChanged+=_dataGridViewQualitySpecifications_CellValueChanged;
			// 
			// _columnImage
			// 
			_columnImage.DataPropertyName="Image";
			_columnImage.HeaderText="";
			_columnImage.MinimumWidth=20;
			_columnImage.Name="_columnImage";
			_columnImage.ReadOnly=true;
			_columnImage.Resizable=System.Windows.Forms.DataGridViewTriState.False;
			_columnImage.Width=30;
			// 
			// _columnName
			// 
			_columnName.AutoSizeMode=System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			_columnName.DataPropertyName="Name";
			_columnName.HeaderText="Name";
			_columnName.MinimumWidth=200;
			_columnName.Name="_columnName";
			_columnName.ReadOnly=true;
			_columnName.Width=200;
			// 
			// _columnType
			// 
			_columnType.DataPropertyName="Type";
			_columnType.HeaderText="Type";
			_columnType.MinimumWidth=8;
			_columnType.Name="_columnType";
			_columnType.ReadOnly=true;
			_columnType.Width=180;
			// 
			// _columnAlgorithm
			// 
			_columnAlgorithm.AutoSizeMode=System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			_columnAlgorithm.DataPropertyName="AlgorithmImplementation";
			_columnAlgorithm.HeaderText="Algorithm";
			_columnAlgorithm.MinimumWidth=8;
			_columnAlgorithm.Name="_columnAlgorithm";
			_columnAlgorithm.Width=86;
			// 
			// _columnDescription
			// 
			_columnDescription.DataPropertyName="Description";
			_columnDescription.HeaderText="Description";
			_columnDescription.MinimumWidth=8;
			_columnDescription.Name="_columnDescription";
			_columnDescription.Width=150;
			// 
			// _columnCategory
			// 
			_columnCategory.AutoSizeMode=System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			_columnCategory.DataPropertyName="Category";
			_columnCategory.HeaderText="Category";
			_columnCategory.MinimumWidth=50;
			_columnCategory.Name="_columnCategory";
			_columnCategory.ReadOnly=true;
			// 
			// _toolStripElements
			// 
			_toolStripElements.AutoSize=false;
			_toolStripElements.BackColor=System.Drawing.SystemColors.ControlLightLight;
			_toolStripElements.ClickThrough=true;
			_toolStripElements.GripStyle=System.Windows.Forms.ToolStripGripStyle.Hidden;
			_toolStripElements.ImageScalingSize=new System.Drawing.Size(24, 24);
			_toolStripElements.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripLabel1 });
			_toolStripElements.Location=new System.Drawing.Point(6, 5);
			_toolStripElements.Name="_toolStripElements";
			_toolStripElements.Padding=new System.Windows.Forms.Padding(0, 0, 3, 0);
			_toolStripElements.Size=new System.Drawing.Size(681, 48);
			_toolStripElements.TabIndex=25;
			_toolStripElements.Text="Element Tools";
			// 
			// toolStripLabel1
			// 
			toolStripLabel1.Name="toolStripLabel1";
			toolStripLabel1.Size=new System.Drawing.Size(164, 45);
			toolStripLabel1.Text="This instance is referenced by:";
			// 
			// miniToolStrip
			// 
			miniToolStrip.AccessibleName="New item selection";
			miniToolStrip.AccessibleRole=System.Windows.Forms.AccessibleRole.ButtonDropDown;
			miniToolStrip.AutoSize=false;
			miniToolStrip.CanOverflow=false;
			miniToolStrip.ClickThrough=true;
			miniToolStrip.Dock=System.Windows.Forms.DockStyle.None;
			miniToolStrip.GripStyle=System.Windows.Forms.ToolStripGripStyle.Hidden;
			miniToolStrip.ImageScalingSize=new System.Drawing.Size(24, 24);
			miniToolStrip.Location=new System.Drawing.Point(159, 10);
			miniToolStrip.Name="miniToolStrip";
			miniToolStrip.Padding=new System.Windows.Forms.Padding(0, 0, 3, 0);
			miniToolStrip.Size=new System.Drawing.Size(989, 48);
			miniToolStrip.TabIndex=25;
			// 
			// _tabPageParameters
			// 
			_tabPageParameters.Controls.Add(_instanceConfigTableViewControlPanel);
			_tabPageParameters.Location=new System.Drawing.Point(4, 24);
			_tabPageParameters.Margin=new System.Windows.Forms.Padding(6, 5, 6, 5);
			_tabPageParameters.Name="_tabPageParameters";
			_tabPageParameters.Padding=new System.Windows.Forms.Padding(6, 5, 6, 5);
			_tabPageParameters.Size=new System.Drawing.Size(693, 322);
			_tabPageParameters.TabIndex=0;
			_tabPageParameters.Text="Parameters";
			_tabPageParameters.UseVisualStyleBackColor=true;
			// 
			// _instanceConfigTableViewControlPanel
			// 
			_instanceConfigTableViewControlPanel.Controls.Add(_splitContainer);
			_instanceConfigTableViewControlPanel.Dock=System.Windows.Forms.DockStyle.Fill;
			_instanceConfigTableViewControlPanel.Location=new System.Drawing.Point(6, 5);
			_instanceConfigTableViewControlPanel.Margin=new System.Windows.Forms.Padding(4, 5, 4, 5);
			_instanceConfigTableViewControlPanel.Name="_instanceConfigTableViewControlPanel";
			_instanceConfigTableViewControlPanel.Size=new System.Drawing.Size(681, 312);
			_instanceConfigTableViewControlPanel.TabIndex=29;
			// 
			// _splitContainer
			// 
			_splitContainer.BackColor=System.Drawing.Color.LightGray;
			_splitContainer.Dock=System.Windows.Forms.DockStyle.Fill;
			_splitContainer.Location=new System.Drawing.Point(0, 0);
			_splitContainer.Margin=new System.Windows.Forms.Padding(6, 5, 6, 5);
			_splitContainer.Name="_splitContainer";
			_splitContainer.Orientation=System.Windows.Forms.Orientation.Horizontal;
			// 
			// _splitContainer.Panel1
			// 
			_splitContainer.Panel1.BackColor=System.Drawing.SystemColors.ControlLightLight;
			_splitContainer.Panel1.Controls.Add(_panelParamsDesc);
			_splitContainer.Panel1.Padding=new System.Windows.Forms.Padding(5);
			// 
			// _splitContainer.Panel2
			// 
			_splitContainer.Panel2.BackColor=System.Drawing.SystemColors.ControlLightLight;
			_splitContainer.Panel2.Controls.Add(_panelParametersEdit);
			_splitContainer.Panel2.Controls.Add(_panelParametersTop);
			_splitContainer.Size=new System.Drawing.Size(681, 312);
			_splitContainer.SplitterDistance=40;
			_splitContainer.SplitterWidth=7;
			_splitContainer.TabIndex=30;
			// 
			// _panelParamsDesc
			// 
			_panelParamsDesc.Controls.Add(_textBoxDescGrid);
			_panelParamsDesc.Controls.Add(labelDescGrid);
			_panelParamsDesc.Dock=System.Windows.Forms.DockStyle.Fill;
			_panelParamsDesc.Location=new System.Drawing.Point(5, 5);
			_panelParamsDesc.Margin=new System.Windows.Forms.Padding(6, 5, 6, 5);
			_panelParamsDesc.Name="_panelParamsDesc";
			_panelParamsDesc.Size=new System.Drawing.Size(671, 30);
			_panelParamsDesc.TabIndex=27;
			// 
			// _textBoxDescGrid
			// 
			_textBoxDescGrid.BackColor=System.Drawing.SystemColors.ControlLightLight;
			_textBoxDescGrid.Dock=System.Windows.Forms.DockStyle.Fill;
			_textBoxDescGrid.Location=new System.Drawing.Point(75, 0);
			_textBoxDescGrid.Margin=new System.Windows.Forms.Padding(6, 5, 6, 5);
			_textBoxDescGrid.Multiline=true;
			_textBoxDescGrid.Name="_textBoxDescGrid";
			_textBoxDescGrid.ReadOnly=true;
			_textBoxDescGrid.Size=new System.Drawing.Size(596, 30);
			_textBoxDescGrid.TabIndex=21;
			// 
			// labelDescGrid
			// 
			labelDescGrid.Dock=System.Windows.Forms.DockStyle.Left;
			labelDescGrid.Location=new System.Drawing.Point(0, 0);
			labelDescGrid.Margin=new System.Windows.Forms.Padding(6, 5, 6, 0);
			labelDescGrid.Name="labelDescGrid";
			labelDescGrid.Size=new System.Drawing.Size(75, 30);
			labelDescGrid.TabIndex=22;
			labelDescGrid.Text="Description:";
			labelDescGrid.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _panelParametersEdit
			// 
			_panelParametersEdit.BackColor=System.Drawing.Color.Transparent;
			_panelParametersEdit.Dock=System.Windows.Forms.DockStyle.Fill;
			_panelParametersEdit.Location=new System.Drawing.Point(0, 27);
			_panelParametersEdit.Margin=new System.Windows.Forms.Padding(0);
			_panelParametersEdit.Name="_panelParametersEdit";
			_panelParametersEdit.Size=new System.Drawing.Size(681, 238);
			_panelParametersEdit.TabIndex=1;
			// 
			// _panelParametersTop
			// 
			_panelParametersTop.BackColor=System.Drawing.Color.Transparent;
			_panelParametersTop.Controls.Add(_linkDocumentation);
			_panelParametersTop.Dock=System.Windows.Forms.DockStyle.Top;
			_panelParametersTop.Location=new System.Drawing.Point(0, 0);
			_panelParametersTop.Name="_panelParametersTop";
			_panelParametersTop.Size=new System.Drawing.Size(681, 27);
			_panelParametersTop.TabIndex=0;
			// 
			// _linkDocumentation
			// 
			_linkDocumentation.AutoSize=true;
			_linkDocumentation.Location=new System.Drawing.Point(6, 9);
			_linkDocumentation.Margin=new System.Windows.Forms.Padding(6, 0, 6, 0);
			_linkDocumentation.Name="_linkDocumentation";
			_linkDocumentation.Size=new System.Drawing.Size(179, 15);
			_linkDocumentation.TabIndex=23;
			_linkDocumentation.TabStop=true;
			_linkDocumentation.Text="Show Parameter Documentation";
			_linkDocumentation.TextAlign=System.Drawing.ContentAlignment.TopRight;
			_linkDocumentation.LinkClicked+=_linkDocumentation_LinkClicked;
			// 
			// _splitContainerHeader
			// 
			_splitContainerHeader.Location=new System.Drawing.Point(0, 0);
			_splitContainerHeader.Name="_splitContainerHeader";
			_splitContainerHeader.Size=new System.Drawing.Size(150, 100);
			_splitContainerHeader.TabIndex=0;
			// 
			// _splitContainerProperties
			// 
			_splitContainerProperties.Dock=System.Windows.Forms.DockStyle.Fill;
			_splitContainerProperties.FixedPanel=System.Windows.Forms.FixedPanel.Panel1;
			_splitContainerProperties.Location=new System.Drawing.Point(6, 5);
			_splitContainerProperties.Margin=new System.Windows.Forms.Padding(6, 5, 6, 5);
			_splitContainerProperties.Name="_splitContainerProperties";
			_splitContainerProperties.Orientation=System.Windows.Forms.Orientation.Horizontal;
			// 
			// _splitContainerProperties.Panel1
			// 
			_splitContainerProperties.Panel1.Controls.Add(_textBoxDescProps);
			_splitContainerProperties.Panel1.Controls.Add(_panelDescriptionLabel);
			// 
			// _splitContainerProperties.Panel2
			// 
			_splitContainerProperties.Panel2.BackColor=System.Drawing.SystemColors.ControlLightLight;
			_splitContainerProperties.Size=new System.Drawing.Size(661, 274);
			_splitContainerProperties.SplitterDistance=74;
			_splitContainerProperties.SplitterWidth=8;
			_splitContainerProperties.TabIndex=31;
			// 
			// _textBoxDescProps
			// 
			_textBoxDescProps.Dock=System.Windows.Forms.DockStyle.Fill;
			_textBoxDescProps.Location=new System.Drawing.Point(123, 0);
			_textBoxDescProps.Margin=new System.Windows.Forms.Padding(6, 5, 6, 5);
			_textBoxDescProps.Multiline=true;
			_textBoxDescProps.Name="_textBoxDescProps";
			_textBoxDescProps.ReadOnly=true;
			_textBoxDescProps.ScrollBars=System.Windows.Forms.ScrollBars.Vertical;
			_textBoxDescProps.Size=new System.Drawing.Size(538, 74);
			_textBoxDescProps.TabIndex=0;
			// 
			// _panelDescriptionLabel
			// 
			_panelDescriptionLabel.Controls.Add(_labelParameterDescription);
			_panelDescriptionLabel.Dock=System.Windows.Forms.DockStyle.Left;
			_panelDescriptionLabel.Location=new System.Drawing.Point(0, 0);
			_panelDescriptionLabel.Margin=new System.Windows.Forms.Padding(6, 5, 6, 5);
			_panelDescriptionLabel.MaximumSize=new System.Drawing.Size(123, 19230);
			_panelDescriptionLabel.MinimumSize=new System.Drawing.Size(123, 0);
			_panelDescriptionLabel.Name="_panelDescriptionLabel";
			_panelDescriptionLabel.Size=new System.Drawing.Size(123, 74);
			_panelDescriptionLabel.TabIndex=23;
			// 
			// _labelParameterDescription
			// 
			_labelParameterDescription.AutoSize=true;
			_labelParameterDescription.Location=new System.Drawing.Point(9, 5);
			_labelParameterDescription.Margin=new System.Windows.Forms.Padding(6, 0, 6, 0);
			_labelParameterDescription.Name="_labelParameterDescription";
			_labelParameterDescription.Size=new System.Drawing.Size(70, 15);
			_labelParameterDescription.TabIndex=22;
			_labelParameterDescription.Text="Description:";
			_labelParameterDescription.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _tabControlDetails
			// 
			_tabControlDetails.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Bottom|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_tabControlDetails.Controls.Add(_tabPageParameters);
			_tabControlDetails.Controls.Add(_tabPageReferencing);
			_tabControlDetails.Controls.Add(_tabPageNotes);
			_tabControlDetails.Location=new System.Drawing.Point(14, 204);
			_tabControlDetails.Margin=new System.Windows.Forms.Padding(6, 5, 6, 5);
			_tabControlDetails.Name="_tabControlDetails";
			_tabControlDetails.SelectedIndex=0;
			_tabControlDetails.Size=new System.Drawing.Size(701, 350);
			_tabControlDetails.TabIndex=0;
			_tabControlDetails.SelectedIndexChanged+=_tabControlDetails_SelectedIndexChanged;
			// 
			// InstanceConfigurationControl
			// 
			AutoScaleDimensions=new System.Drawing.SizeF(7F, 15F);
			AutoScaleMode=System.Windows.Forms.AutoScaleMode.Font;
			Controls.Add(_textBoxDescription);
			Controls.Add(_textBoxUrl);
			Controls.Add(_textBoxName);
			Controls.Add(_objectReferenceControlInstanceDescriptor);
			Controls.Add(_textBoxQualitySpecifications);
			Controls.Add(_buttonOpenUrl);
			Controls.Add(_tabControlDetails);
			Controls.Add(_labelQualitySpecifications);
			Controls.Add(_labelCategory);
			Controls.Add(_textBoxCategory);
			Controls.Add(_labelInstanceDescriptor);
			Controls.Add(_buttonGoToInstanceDescriptor);
			Controls.Add(_labelDescription);
			Controls.Add(_labelUrl);
			Controls.Add(_labelName);
			Margin=new System.Windows.Forms.Padding(6, 5, 6, 5);
			Name="InstanceConfigurationControl";
			Size=new System.Drawing.Size(721, 559);
			Paint+=QualityConditionControl_Paint;
			((System.ComponentModel.ISupportInitialize)_errorProvider).EndInit();
			_tabPageNotes.ResumeLayout(false);
			_tabPageNotes.PerformLayout();
			_tabPageReferencing.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)_dataGridViewReferences).EndInit();
			_toolStripElements.ResumeLayout(false);
			_toolStripElements.PerformLayout();
			_tabPageParameters.ResumeLayout(false);
			_instanceConfigTableViewControlPanel.ResumeLayout(false);
			_splitContainer.Panel1.ResumeLayout(false);
			_splitContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)_splitContainer).EndInit();
			_splitContainer.ResumeLayout(false);
			_panelParamsDesc.ResumeLayout(false);
			_panelParamsDesc.PerformLayout();
			_panelParametersTop.ResumeLayout(false);
			_panelParametersTop.PerformLayout();
			((System.ComponentModel.ISupportInitialize)_splitContainerHeader).EndInit();
			_splitContainerHeader.ResumeLayout(false);
			_splitContainerProperties.Panel1.ResumeLayout(false);
			_splitContainerProperties.Panel1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)_splitContainerProperties).EndInit();
			_splitContainerProperties.ResumeLayout(false);
			_panelDescriptionLabel.ResumeLayout(false);
			_panelDescriptionLabel.PerformLayout();
			_tabControlDetails.ResumeLayout(false);
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private System.Windows.Forms.TextBox _textBoxName;
		private System.Windows.Forms.Label _labelName;
		private System.Windows.Forms.TextBox _textBoxDescription;
		private System.Windows.Forms.Label _labelDescription;
		private System.Windows.Forms.ErrorProvider _errorProvider;
		private ObjectReferenceControl _objectReferenceControlInstanceDescriptor;
		private System.Windows.Forms.Label labelDescGrid;
		private System.Windows.Forms.TextBox _textBoxDescGrid;
		private System.Windows.Forms.Label _labelInstanceDescriptor;
		private System.Windows.Forms.Button _buttonGoToInstanceDescriptor;
		private System.Windows.Forms.OpenFileDialog openFileDialogImport;
		private System.Windows.Forms.SaveFileDialog saveFileDialogExport;
		private global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx _splitContainer;
		private global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx _splitContainerProperties;
		private System.Windows.Forms.Label _labelParameterDescription;
		private System.Windows.Forms.TextBox _textBoxDescProps;
		private System.Windows.Forms.TabControl _tabControlDetails;
		private System.Windows.Forms.TabPage _tabPageParameters;
		private System.Windows.Forms.TabPage _tabPageReferencing;
		private System.Windows.Forms.ToolStripLabel toolStripLabel1;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
		private System.Windows.Forms.TextBox _textBoxQualitySpecifications;
		private System.Windows.Forms.Label _labelQualitySpecifications;
		private DoubleBufferedDataGridView _dataGridViewReferences;
		private ToolStripEx _toolStripElements;
		private System.Windows.Forms.Label _labelUrl;
		private System.Windows.Forms.TextBox _textBoxUrl;
		private System.Windows.Forms.Button _buttonOpenUrl;
		private System.Windows.Forms.Panel _panelDescriptionLabel;
		private global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx _splitContainerHeader;
		private System.Windows.Forms.Label _labelCategory;
		private System.Windows.Forms.TextBox _textBoxCategory;
		private System.Windows.Forms.ToolTip _toolTip;
		private System.Windows.Forms.TabPage _tabPageNotes;
		private System.Windows.Forms.TextBox _textBoxNotes;
		private System.Windows.Forms.Panel _instanceConfigTableViewControlPanel;
		private System.Windows.Forms.DataGridViewImageColumn _columnImage;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnName;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnType;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnCategory;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnAlgorithm;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnDescription;
		private ToolStripEx miniToolStrip;
		private System.Windows.Forms.LinkLabel _linkDocumentation;
		private System.Windows.Forms.Panel _panelParametersTop;
		private System.Windows.Forms.Panel _panelParametersEdit;
		private System.Windows.Forms.Panel _panelParamsDesc;
	}
}
