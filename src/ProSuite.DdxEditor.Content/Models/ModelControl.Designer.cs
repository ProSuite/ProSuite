using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Models
{
    partial class ModelControl<T> where T : DdxModel
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
			this._labelName = new System.Windows.Forms.Label();
			this._textBoxName = new System.Windows.Forms.TextBox();
			this._labelSpatialReferenceDescriptor = new System.Windows.Forms.Label();
			this._labelUserConnectionProvider = new System.Windows.Forms.Label();
			this._labelRepositoryOwnerConnectionProvider = new System.Windows.Forms.Label();
			this._labelSchemaOwnerConnectionProvider = new System.Windows.Forms.Label();
			this._textBoxDescription = new System.Windows.Forms.TextBox();
			this._labelDescription = new System.Windows.Forms.Label();
			this._textBoxDatasetPrefix = new System.Windows.Forms.TextBox();
			this._labelDatasetPrefix = new System.Windows.Forms.Label();
			this._labelDefaultMinimumSegmentLength = new System.Windows.Forms.Label();
			this._numericUpDownDefaultMinimumSegmentLength = new System.Windows.Forms.NumericUpDown();
			this._checkBoxUpdateAliasNamesOnHarvest = new System.Windows.Forms.CheckBox();
			this._checkBoxHarvestQualifiedElementNames = new System.Windows.Forms.CheckBox();
			this._checkBoxIgnoreUnversionedDatasets = new System.Windows.Forms.CheckBox();
			this._labelDatasetListBuilderFactoryClassDescriptor = new System.Windows.Forms.Label();
			this._labelAttributeConfiguratorFactoryClassDescriptor = new System.Windows.Forms.Label();
			this._tabControl = new System.Windows.Forms.TabControl();
			this._tabPageDefaultDatabase = new System.Windows.Forms.TabPage();
			this._labelUseDefaultDatabaseForSchemaOnly = new System.Windows.Forms.Label();
			this._booleanComboboxUseDefaultDatabaseForSchemaOnly = new ProSuite.Commons.UI.WinForms.Controls.BooleanCombobox();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this._labelElementNameQualificationStatus = new System.Windows.Forms.Label();
			this._textBoxLastHarvestedByUser = new System.Windows.Forms.TextBox();
			this._labelLastHarvestedByUser = new System.Windows.Forms.Label();
			this._textBoxLastHarvestedConnectionString = new System.Windows.Forms.TextBox();
			this._textBoxLastHarvestedDate = new System.Windows.Forms.TextBox();
			this._labelLastHarvestedConnectionString = new System.Windows.Forms.Label();
			this._labelLastHarvestedDate = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this._textBoxDefaultDatabaseName = new System.Windows.Forms.TextBox();
			this._textBoxDefaultDatabaseSchemaOwner = new System.Windows.Forms.TextBox();
			this._labelDefaultDatabaseName = new System.Windows.Forms.Label();
			this._labelDefaultDatabaseSchemaOwner = new System.Windows.Forms.Label();
			this._objectReferenceControlUserConnectionProvider = new ProSuite.Commons.UI.WinForms.Controls.ObjectReferenceControl();
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this._buttonHarvestingPreview = new System.Windows.Forms.Button();
			this._textBoxSchemaOwner = new System.Windows.Forms.TextBox();
			this._labelSchemaOwner = new System.Windows.Forms.Label();
			this._textBoxDatasetExclusionCriteria = new System.Windows.Forms.TextBox();
			this._textBoxDatasetInclusionCriteria = new System.Windows.Forms.TextBox();
			this._labelDatasetInclusionCriteria = new System.Windows.Forms.Label();
			this._labelDatasetExclusionCriteria = new System.Windows.Forms.Label();
			this._checkboxIgnoreUnregisteredTables = new System.Windows.Forms.CheckBox();
			this._tabPageAdvanced = new System.Windows.Forms.TabPage();
			this.label3 = new System.Windows.Forms.Label();
			this._comboBoxSqlCaseSensitivity = new System.Windows.Forms.ComboBox();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this._objectReferenceControlSchemaOwnerConnectionProvider = new ProSuite.Commons.UI.WinForms.Controls.ObjectReferenceControl();
			this._objectReferenceControlRepositoryOwnerConnectionProvider = new ProSuite.Commons.UI.WinForms.Controls.ObjectReferenceControl();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this._objectReferenceControlDatasetListBuilderFactoryClassDescriptor = new ProSuite.Commons.UI.WinForms.Controls.ObjectReferenceControl();
			this._objectReferenceControlAttributeConfiguratorFactoryClassDescriptor = new ProSuite.Commons.UI.WinForms.Controls.ObjectReferenceControl();
			this._labelSqlCaseSensitivity = new System.Windows.Forms.Label();
			this._objectReferenceControlSpatialReferenceDescriptor = new ProSuite.Commons.UI.WinForms.Controls.ObjectReferenceControl();
			this._buttonGoToSpatialReference = new System.Windows.Forms.Button();
			this._buttonGoToUserConnectionProvider = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this._numericUpDownDefaultMinimumSegmentLength)).BeginInit();
			this._tabControl.SuspendLayout();
			this._tabPageDefaultDatabase.SuspendLayout();
			this.groupBox4.SuspendLayout();
			this.tabPage2.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this._tabPageAdvanced.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// _labelName
			// 
			this._labelName.AutoSize = true;
			this._labelName.Location = new System.Drawing.Point(81, 6);
			this._labelName.Name = "_labelName";
			this._labelName.Size = new System.Drawing.Size(38, 13);
			this._labelName.TabIndex = 0;
			this._labelName.Text = "Name:";
			this._labelName.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxName
			// 
			this._textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxName.Location = new System.Drawing.Point(125, 3);
			this._textBoxName.Name = "_textBoxName";
			this._textBoxName.Size = new System.Drawing.Size(527, 20);
			this._textBoxName.TabIndex = 0;
			// 
			// _labelSpatialReferenceDescriptor
			// 
			this._labelSpatialReferenceDescriptor.AutoSize = true;
			this._labelSpatialReferenceDescriptor.Location = new System.Drawing.Point(29, 103);
			this._labelSpatialReferenceDescriptor.Name = "_labelSpatialReferenceDescriptor";
			this._labelSpatialReferenceDescriptor.Size = new System.Drawing.Size(90, 13);
			this._labelSpatialReferenceDescriptor.TabIndex = 29;
			this._labelSpatialReferenceDescriptor.Text = "Spatial reference:";
			this._labelSpatialReferenceDescriptor.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelUserConnectionProvider
			// 
			this._labelUserConnectionProvider.AutoSize = true;
			this._labelUserConnectionProvider.Location = new System.Drawing.Point(7, 23);
			this._labelUserConnectionProvider.Name = "_labelUserConnectionProvider";
			this._labelUserConnectionProvider.Size = new System.Drawing.Size(105, 13);
			this._labelUserConnectionProvider.TabIndex = 33;
			this._labelUserConnectionProvider.Text = "Connection provider:";
			this._labelUserConnectionProvider.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelRepositoryOwnerConnectionProvider
			// 
			this._labelRepositoryOwnerConnectionProvider.AutoSize = true;
			this._labelRepositoryOwnerConnectionProvider.Location = new System.Drawing.Point(18, 54);
			this._labelRepositoryOwnerConnectionProvider.Name = "_labelRepositoryOwnerConnectionProvider";
			this._labelRepositoryOwnerConnectionProvider.Size = new System.Drawing.Size(168, 13);
			this._labelRepositoryOwnerConnectionProvider.TabIndex = 34;
			this._labelRepositoryOwnerConnectionProvider.Text = "SDE repository owner connection:";
			this._labelRepositoryOwnerConnectionProvider.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelSchemaOwnerConnectionProvider
			// 
			this._labelSchemaOwnerConnectionProvider.AutoSize = true;
			this._labelSchemaOwnerConnectionProvider.Location = new System.Drawing.Point(49, 28);
			this._labelSchemaOwnerConnectionProvider.Name = "_labelSchemaOwnerConnectionProvider";
			this._labelSchemaOwnerConnectionProvider.Size = new System.Drawing.Size(137, 13);
			this._labelSchemaOwnerConnectionProvider.TabIndex = 34;
			this._labelSchemaOwnerConnectionProvider.Text = "Schema owner connection:";
			this._labelSchemaOwnerConnectionProvider.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxDescription
			// 
			this._textBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxDescription.Location = new System.Drawing.Point(125, 29);
			this._textBoxDescription.Multiline = true;
			this._textBoxDescription.Name = "_textBoxDescription";
			this._textBoxDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this._textBoxDescription.Size = new System.Drawing.Size(527, 65);
			this._textBoxDescription.TabIndex = 1;
			// 
			// _labelDescription
			// 
			this._labelDescription.AutoSize = true;
			this._labelDescription.Location = new System.Drawing.Point(56, 32);
			this._labelDescription.Name = "_labelDescription";
			this._labelDescription.Size = new System.Drawing.Size(63, 13);
			this._labelDescription.TabIndex = 39;
			this._labelDescription.Text = "Description:";
			this._labelDescription.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxDatasetPrefix
			// 
			this._textBoxDatasetPrefix.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this._textBoxDatasetPrefix.Location = new System.Drawing.Point(419, 19);
			this._textBoxDatasetPrefix.Name = "_textBoxDatasetPrefix";
			this._textBoxDatasetPrefix.Size = new System.Drawing.Size(127, 20);
			this._textBoxDatasetPrefix.TabIndex = 1;
			// 
			// _labelDatasetPrefix
			// 
			this._labelDatasetPrefix.AutoSize = true;
			this._labelDatasetPrefix.Location = new System.Drawing.Point(338, 22);
			this._labelDatasetPrefix.Name = "_labelDatasetPrefix";
			this._labelDatasetPrefix.Size = new System.Drawing.Size(75, 13);
			this._labelDatasetPrefix.TabIndex = 39;
			this._labelDatasetPrefix.Text = "Dataset prefix:";
			this._labelDatasetPrefix.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelDefaultMinimumSegmentLength
			// 
			this._labelDefaultMinimumSegmentLength.AutoSize = true;
			this._labelDefaultMinimumSegmentLength.Location = new System.Drawing.Point(32, 43);
			this._labelDefaultMinimumSegmentLength.Name = "_labelDefaultMinimumSegmentLength";
			this._labelDefaultMinimumSegmentLength.Size = new System.Drawing.Size(162, 13);
			this._labelDefaultMinimumSegmentLength.TabIndex = 29;
			this._labelDefaultMinimumSegmentLength.Text = "Default minimum segment length:";
			this._labelDefaultMinimumSegmentLength.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _numericUpDownDefaultMinimumSegmentLength
			// 
			this._numericUpDownDefaultMinimumSegmentLength.DecimalPlaces = 2;
			this._numericUpDownDefaultMinimumSegmentLength.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
			this._numericUpDownDefaultMinimumSegmentLength.Location = new System.Drawing.Point(200, 41);
			this._numericUpDownDefaultMinimumSegmentLength.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
			this._numericUpDownDefaultMinimumSegmentLength.Name = "_numericUpDownDefaultMinimumSegmentLength";
			this._numericUpDownDefaultMinimumSegmentLength.Size = new System.Drawing.Size(76, 20);
			this._numericUpDownDefaultMinimumSegmentLength.TabIndex = 1;
			// 
			// _checkBoxUpdateAliasNamesOnHarvest
			// 
			this._checkBoxUpdateAliasNamesOnHarvest.AutoSize = true;
			this._checkBoxUpdateAliasNamesOnHarvest.Location = new System.Drawing.Point(19, 41);
			this._checkBoxUpdateAliasNamesOnHarvest.Name = "_checkBoxUpdateAliasNamesOnHarvest";
			this._checkBoxUpdateAliasNamesOnHarvest.Size = new System.Drawing.Size(159, 17);
			this._checkBoxUpdateAliasNamesOnHarvest.TabIndex = 1;
			this._checkBoxUpdateAliasNamesOnHarvest.Text = "Harvest dataset alias names";
			this._checkBoxUpdateAliasNamesOnHarvest.UseVisualStyleBackColor = true;
			// 
			// _checkBoxHarvestQualifiedElementNames
			// 
			this._checkBoxHarvestQualifiedElementNames.AutoSize = true;
			this._checkBoxHarvestQualifiedElementNames.Location = new System.Drawing.Point(19, 18);
			this._checkBoxHarvestQualifiedElementNames.Name = "_checkBoxHarvestQualifiedElementNames";
			this._checkBoxHarvestQualifiedElementNames.Size = new System.Drawing.Size(177, 17);
			this._checkBoxHarvestQualifiedElementNames.TabIndex = 0;
			this._checkBoxHarvestQualifiedElementNames.Text = "Harvest qualified dataset names";
			this._checkBoxHarvestQualifiedElementNames.UseVisualStyleBackColor = true;
			// 
			// _checkBoxIgnoreUnversionedDatasets
			// 
			this._checkBoxIgnoreUnversionedDatasets.AutoSize = true;
			this._checkBoxIgnoreUnversionedDatasets.Location = new System.Drawing.Point(156, 147);
			this._checkBoxIgnoreUnversionedDatasets.Name = "_checkBoxIgnoreUnversionedDatasets";
			this._checkBoxIgnoreUnversionedDatasets.Size = new System.Drawing.Size(201, 17);
			this._checkBoxIgnoreUnversionedDatasets.TabIndex = 4;
			this._checkBoxIgnoreUnversionedDatasets.Text = "Ignore unversioned ArcSDE datasets";
			this._checkBoxIgnoreUnversionedDatasets.UseVisualStyleBackColor = true;
			// 
			// _labelDatasetListBuilderFactoryClassDescriptor
			// 
			this._labelDatasetListBuilderFactoryClassDescriptor.AutoSize = true;
			this._labelDatasetListBuilderFactoryClassDescriptor.Location = new System.Drawing.Point(55, 54);
			this._labelDatasetListBuilderFactoryClassDescriptor.Name = "_labelDatasetListBuilderFactoryClassDescriptor";
			this._labelDatasetListBuilderFactoryClassDescriptor.Size = new System.Drawing.Size(131, 13);
			this._labelDatasetListBuilderFactoryClassDescriptor.TabIndex = 2;
			this._labelDatasetListBuilderFactoryClassDescriptor.Text = "Dataset list builder factory:";
			this._labelDatasetListBuilderFactoryClassDescriptor.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelAttributeConfiguratorFactoryClassDescriptor
			// 
			this._labelAttributeConfiguratorFactoryClassDescriptor.AutoSize = true;
			this._labelAttributeConfiguratorFactoryClassDescriptor.Location = new System.Drawing.Point(43, 28);
			this._labelAttributeConfiguratorFactoryClassDescriptor.Name = "_labelAttributeConfiguratorFactoryClassDescriptor";
			this._labelAttributeConfiguratorFactoryClassDescriptor.Size = new System.Drawing.Size(143, 13);
			this._labelAttributeConfiguratorFactoryClassDescriptor.TabIndex = 2;
			this._labelAttributeConfiguratorFactoryClassDescriptor.Text = "Attribute configurator factory:";
			this._labelAttributeConfiguratorFactoryClassDescriptor.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _tabControl
			// 
			this._tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._tabControl.Controls.Add(this._tabPageDefaultDatabase);
			this._tabControl.Controls.Add(this.tabPage2);
			this._tabControl.Controls.Add(this._tabPageAdvanced);
			this._tabControl.Location = new System.Drawing.Point(3, 126);
			this._tabControl.Name = "_tabControl";
			this._tabControl.SelectedIndex = 0;
			this._tabControl.Size = new System.Drawing.Size(665, 300);
			this._tabControl.TabIndex = 3;
			this._tabControl.SelectedIndexChanged += new System.EventHandler(this._tabControl_SelectedIndexChanged);
			// 
			// _tabPageDefaultDatabase
			// 
			this._tabPageDefaultDatabase.Controls.Add(this._labelUseDefaultDatabaseForSchemaOnly);
			this._tabPageDefaultDatabase.Controls.Add(this._booleanComboboxUseDefaultDatabaseForSchemaOnly);
			this._tabPageDefaultDatabase.Controls.Add(this.groupBox4);
			this._tabPageDefaultDatabase.Controls.Add(this._labelUserConnectionProvider);
			this._tabPageDefaultDatabase.Controls.Add(this._buttonGoToUserConnectionProvider);
			this._tabPageDefaultDatabase.Controls.Add(this._objectReferenceControlUserConnectionProvider);
			this._tabPageDefaultDatabase.Location = new System.Drawing.Point(4, 22);
			this._tabPageDefaultDatabase.Name = "_tabPageDefaultDatabase";
			this._tabPageDefaultDatabase.Padding = new System.Windows.Forms.Padding(3);
			this._tabPageDefaultDatabase.Size = new System.Drawing.Size(657, 274);
			this._tabPageDefaultDatabase.TabIndex = 0;
			this._tabPageDefaultDatabase.Text = "Master Database";
			this._tabPageDefaultDatabase.UseVisualStyleBackColor = true;
			// 
			// _labelUseDefaultDatabaseForSchemaOnly
			// 
			this._labelUseDefaultDatabaseForSchemaOnly.AutoSize = true;
			this._labelUseDefaultDatabaseForSchemaOnly.Location = new System.Drawing.Point(117, 49);
			this._labelUseDefaultDatabaseForSchemaOnly.Name = "_labelUseDefaultDatabaseForSchemaOnly";
			this._labelUseDefaultDatabaseForSchemaOnly.Size = new System.Drawing.Size(125, 13);
			this._labelUseDefaultDatabaseForSchemaOnly.TabIndex = 47;
			this._labelUseDefaultDatabaseForSchemaOnly.Text = "Use master database for:";
			this._labelUseDefaultDatabaseForSchemaOnly.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _booleanComboboxUseDefaultDatabaseForSchemaOnly
			// 
			this._booleanComboboxUseDefaultDatabaseForSchemaOnly.FalseText = "Schema and data (central database)";
			this._booleanComboboxUseDefaultDatabaseForSchemaOnly.FlatStyle = System.Windows.Forms.FlatStyle.Standard;
			this._booleanComboboxUseDefaultDatabaseForSchemaOnly.Location = new System.Drawing.Point(248, 45);
			this._booleanComboboxUseDefaultDatabaseForSchemaOnly.Name = "_booleanComboboxUseDefaultDatabaseForSchemaOnly";
			this._booleanComboboxUseDefaultDatabaseForSchemaOnly.Size = new System.Drawing.Size(257, 21);
			this._booleanComboboxUseDefaultDatabaseForSchemaOnly.TabIndex = 1;
			this._booleanComboboxUseDefaultDatabaseForSchemaOnly.TrueText = "Schema only";
			this._booleanComboboxUseDefaultDatabaseForSchemaOnly.Value = false;
			// 
			// groupBox4
			// 
			this.groupBox4.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox4.Controls.Add(this._labelElementNameQualificationStatus);
			this.groupBox4.Controls.Add(this._textBoxLastHarvestedByUser);
			this.groupBox4.Controls.Add(this._labelLastHarvestedByUser);
			this.groupBox4.Controls.Add(this._textBoxLastHarvestedConnectionString);
			this.groupBox4.Controls.Add(this._textBoxLastHarvestedDate);
			this.groupBox4.Controls.Add(this._labelLastHarvestedConnectionString);
			this.groupBox4.Controls.Add(this._labelLastHarvestedDate);
			this.groupBox4.Controls.Add(this.label2);
			this.groupBox4.Controls.Add(this.label1);
			this.groupBox4.Controls.Add(this._textBoxDefaultDatabaseName);
			this.groupBox4.Controls.Add(this._textBoxDefaultDatabaseSchemaOwner);
			this.groupBox4.Controls.Add(this._labelDefaultDatabaseName);
			this.groupBox4.Controls.Add(this._labelDefaultDatabaseSchemaOwner);
			this.groupBox4.Location = new System.Drawing.Point(6, 82);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(645, 186);
			this.groupBox4.TabIndex = 45;
			this.groupBox4.TabStop = false;
			this.groupBox4.Text = "Harvested model content";
			// 
			// _labelElementNameQualificationStatus
			// 
			this._labelElementNameQualificationStatus.AutoSize = true;
			this._labelElementNameQualificationStatus.Location = new System.Drawing.Point(111, 139);
			this._labelElementNameQualificationStatus.Name = "_labelElementNameQualificationStatus";
			this._labelElementNameQualificationStatus.Size = new System.Drawing.Size(175, 13);
			this._labelElementNameQualificationStatus.TabIndex = 52;
			this._labelElementNameQualificationStatus.Text = "<element name qualification status>";
			// 
			// _textBoxLastHarvestedByUser
			// 
			this._textBoxLastHarvestedByUser.Location = new System.Drawing.Point(323, 27);
			this._textBoxLastHarvestedByUser.Name = "_textBoxLastHarvestedByUser";
			this._textBoxLastHarvestedByUser.ReadOnly = true;
			this._textBoxLastHarvestedByUser.Size = new System.Drawing.Size(100, 20);
			this._textBoxLastHarvestedByUser.TabIndex = 51;
			this._textBoxLastHarvestedByUser.TabStop = false;
			// 
			// _labelLastHarvestedByUser
			// 
			this._labelLastHarvestedByUser.AutoSize = true;
			this._labelLastHarvestedByUser.Location = new System.Drawing.Point(296, 30);
			this._labelLastHarvestedByUser.Name = "_labelLastHarvestedByUser";
			this._labelLastHarvestedByUser.Size = new System.Drawing.Size(21, 13);
			this._labelLastHarvestedByUser.TabIndex = 50;
			this._labelLastHarvestedByUser.Text = "by:";
			this._labelLastHarvestedByUser.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxLastHarvestedConnectionString
			// 
			this._textBoxLastHarvestedConnectionString.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxLastHarvestedConnectionString.Location = new System.Drawing.Point(114, 53);
			this._textBoxLastHarvestedConnectionString.Name = "_textBoxLastHarvestedConnectionString";
			this._textBoxLastHarvestedConnectionString.ReadOnly = true;
			this._textBoxLastHarvestedConnectionString.Size = new System.Drawing.Size(519, 20);
			this._textBoxLastHarvestedConnectionString.TabIndex = 49;
			this._textBoxLastHarvestedConnectionString.TabStop = false;
			// 
			// _textBoxLastHarvestedDate
			// 
			this._textBoxLastHarvestedDate.Location = new System.Drawing.Point(114, 27);
			this._textBoxLastHarvestedDate.Name = "_textBoxLastHarvestedDate";
			this._textBoxLastHarvestedDate.ReadOnly = true;
			this._textBoxLastHarvestedDate.Size = new System.Drawing.Size(176, 20);
			this._textBoxLastHarvestedDate.TabIndex = 48;
			this._textBoxLastHarvestedDate.TabStop = false;
			// 
			// _labelLastHarvestedConnectionString
			// 
			this._labelLastHarvestedConnectionString.AutoSize = true;
			this._labelLastHarvestedConnectionString.Location = new System.Drawing.Point(16, 56);
			this._labelLastHarvestedConnectionString.Name = "_labelLastHarvestedConnectionString";
			this._labelLastHarvestedConnectionString.Size = new System.Drawing.Size(92, 13);
			this._labelLastHarvestedConnectionString.TabIndex = 47;
			this._labelLastHarvestedConnectionString.Text = "Connection string:";
			this._labelLastHarvestedConnectionString.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelLastHarvestedDate
			// 
			this._labelLastHarvestedDate.AutoSize = true;
			this._labelLastHarvestedDate.Location = new System.Drawing.Point(28, 30);
			this._labelLastHarvestedDate.Name = "_labelLastHarvestedDate";
			this._labelLastHarvestedDate.Size = new System.Drawing.Size(80, 13);
			this._labelLastHarvestedDate.TabIndex = 47;
			this._labelLastHarvestedDate.Text = "Last harvested:";
			this._labelLastHarvestedDate.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// label2
			// 
			this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.label2.AutoEllipsis = true;
			this.label2.Location = new System.Drawing.Point(296, 109);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(343, 20);
			this.label2.TabIndex = 46;
			this.label2.Text = "for ArcSDE databases. Empty if datasets are from multiple schemas";
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.label1.AutoEllipsis = true;
			this.label1.Location = new System.Drawing.Point(296, 82);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(343, 20);
			this.label1.TabIndex = 46;
			this.label1.Text = "for SQL Server and PostgreSQL databases";
			// 
			// _textBoxDefaultDatabaseName
			// 
			this._textBoxDefaultDatabaseName.Location = new System.Drawing.Point(114, 79);
			this._textBoxDefaultDatabaseName.Name = "_textBoxDefaultDatabaseName";
			this._textBoxDefaultDatabaseName.ReadOnly = true;
			this._textBoxDefaultDatabaseName.Size = new System.Drawing.Size(176, 20);
			this._textBoxDefaultDatabaseName.TabIndex = 43;
			this._textBoxDefaultDatabaseName.TabStop = false;
			// 
			// _textBoxDefaultDatabaseSchemaOwner
			// 
			this._textBoxDefaultDatabaseSchemaOwner.Location = new System.Drawing.Point(114, 106);
			this._textBoxDefaultDatabaseSchemaOwner.Name = "_textBoxDefaultDatabaseSchemaOwner";
			this._textBoxDefaultDatabaseSchemaOwner.ReadOnly = true;
			this._textBoxDefaultDatabaseSchemaOwner.Size = new System.Drawing.Size(176, 20);
			this._textBoxDefaultDatabaseSchemaOwner.TabIndex = 44;
			this._textBoxDefaultDatabaseSchemaOwner.TabStop = false;
			// 
			// _labelDefaultDatabaseName
			// 
			this._labelDefaultDatabaseName.AutoSize = true;
			this._labelDefaultDatabaseName.Location = new System.Drawing.Point(21, 82);
			this._labelDefaultDatabaseName.Name = "_labelDefaultDatabaseName";
			this._labelDefaultDatabaseName.Size = new System.Drawing.Size(85, 13);
			this._labelDefaultDatabaseName.TabIndex = 41;
			this._labelDefaultDatabaseName.Text = "Database name:";
			this._labelDefaultDatabaseName.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelDefaultDatabaseSchemaOwner
			// 
			this._labelDefaultDatabaseSchemaOwner.AutoSize = true;
			this._labelDefaultDatabaseSchemaOwner.Location = new System.Drawing.Point(25, 109);
			this._labelDefaultDatabaseSchemaOwner.Name = "_labelDefaultDatabaseSchemaOwner";
			this._labelDefaultDatabaseSchemaOwner.Size = new System.Drawing.Size(81, 13);
			this._labelDefaultDatabaseSchemaOwner.TabIndex = 42;
			this._labelDefaultDatabaseSchemaOwner.Text = "Schema owner:";
			this._labelDefaultDatabaseSchemaOwner.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _objectReferenceControlUserConnectionProvider
			// 
			this._objectReferenceControlUserConnectionProvider.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._objectReferenceControlUserConnectionProvider.DataSource = null;
			this._objectReferenceControlUserConnectionProvider.DisplayMember = null;
			this._objectReferenceControlUserConnectionProvider.FindObjectDelegate = null;
			this._objectReferenceControlUserConnectionProvider.FormatTextDelegate = null;
			this._objectReferenceControlUserConnectionProvider.Location = new System.Drawing.Point(144, 19);
			this._objectReferenceControlUserConnectionProvider.Name = "_objectReferenceControlUserConnectionProvider";
			this._objectReferenceControlUserConnectionProvider.ReadOnly = false;
			this._objectReferenceControlUserConnectionProvider.Size = new System.Drawing.Size(495, 20);
			this._objectReferenceControlUserConnectionProvider.TabIndex = 0;
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.Add(this.groupBox2);
			this.tabPage2.Controls.Add(this._checkBoxUpdateAliasNamesOnHarvest);
			this.tabPage2.Controls.Add(this._checkBoxHarvestQualifiedElementNames);
			this.tabPage2.Location = new System.Drawing.Point(4, 22);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage2.Size = new System.Drawing.Size(657, 274);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Text = "Harvesting Options";
			this.tabPage2.UseVisualStyleBackColor = true;
			// 
			// groupBox2
			// 
			this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox2.Controls.Add(this._buttonHarvestingPreview);
			this.groupBox2.Controls.Add(this._textBoxSchemaOwner);
			this.groupBox2.Controls.Add(this._labelSchemaOwner);
			this.groupBox2.Controls.Add(this._textBoxDatasetExclusionCriteria);
			this.groupBox2.Controls.Add(this._textBoxDatasetInclusionCriteria);
			this.groupBox2.Controls.Add(this._labelDatasetInclusionCriteria);
			this.groupBox2.Controls.Add(this._textBoxDatasetPrefix);
			this.groupBox2.Controls.Add(this._labelDatasetExclusionCriteria);
			this.groupBox2.Controls.Add(this._labelDatasetPrefix);
			this.groupBox2.Controls.Add(this._checkboxIgnoreUnregisteredTables);
			this.groupBox2.Controls.Add(this._checkBoxIgnoreUnversionedDatasets);
			this.groupBox2.Location = new System.Drawing.Point(8, 64);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(643, 201);
			this.groupBox2.TabIndex = 42;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Dataset filters";
			// 
			// _buttonHarvestingPreview
			// 
			this._buttonHarvestingPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonHarvestingPreview.Location = new System.Drawing.Point(547, 166);
			this._buttonHarvestingPreview.Name = "_buttonHarvestingPreview";
			this._buttonHarvestingPreview.Size = new System.Drawing.Size(84, 23);
			this._buttonHarvestingPreview.TabIndex = 44;
			this._buttonHarvestingPreview.Text = "Preview";
			this._buttonHarvestingPreview.UseVisualStyleBackColor = true;
			this._buttonHarvestingPreview.Visible = false;
			this._buttonHarvestingPreview.Click += new System.EventHandler(this._buttonHarvestingPreview_Click);
			// 
			// _textBoxSchemaOwner
			// 
			this._textBoxSchemaOwner.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this._textBoxSchemaOwner.Location = new System.Drawing.Point(156, 19);
			this._textBoxSchemaOwner.Name = "_textBoxSchemaOwner";
			this._textBoxSchemaOwner.Size = new System.Drawing.Size(176, 20);
			this._textBoxSchemaOwner.TabIndex = 0;
			// 
			// _labelSchemaOwner
			// 
			this._labelSchemaOwner.AutoSize = true;
			this._labelSchemaOwner.Location = new System.Drawing.Point(69, 22);
			this._labelSchemaOwner.Name = "_labelSchemaOwner";
			this._labelSchemaOwner.Size = new System.Drawing.Size(81, 13);
			this._labelSchemaOwner.TabIndex = 43;
			this._labelSchemaOwner.Text = "Schema owner:";
			this._labelSchemaOwner.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxDatasetExclusionCriteria
			// 
			this._textBoxDatasetExclusionCriteria.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxDatasetExclusionCriteria.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this._textBoxDatasetExclusionCriteria.Location = new System.Drawing.Point(156, 96);
			this._textBoxDatasetExclusionCriteria.Multiline = true;
			this._textBoxDatasetExclusionCriteria.Name = "_textBoxDatasetExclusionCriteria";
			this._textBoxDatasetExclusionCriteria.Size = new System.Drawing.Size(475, 45);
			this._textBoxDatasetExclusionCriteria.TabIndex = 3;
			// 
			// _textBoxDatasetInclusionCriteria
			// 
			this._textBoxDatasetInclusionCriteria.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxDatasetInclusionCriteria.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this._textBoxDatasetInclusionCriteria.Location = new System.Drawing.Point(156, 45);
			this._textBoxDatasetInclusionCriteria.Multiline = true;
			this._textBoxDatasetInclusionCriteria.Name = "_textBoxDatasetInclusionCriteria";
			this._textBoxDatasetInclusionCriteria.Size = new System.Drawing.Size(475, 45);
			this._textBoxDatasetInclusionCriteria.TabIndex = 2;
			// 
			// _labelDatasetInclusionCriteria
			// 
			this._labelDatasetInclusionCriteria.AutoSize = true;
			this._labelDatasetInclusionCriteria.Location = new System.Drawing.Point(25, 48);
			this._labelDatasetInclusionCriteria.Name = "_labelDatasetInclusionCriteria";
			this._labelDatasetInclusionCriteria.Size = new System.Drawing.Size(125, 13);
			this._labelDatasetInclusionCriteria.TabIndex = 39;
			this._labelDatasetInclusionCriteria.Text = "Dataset inclusion criteria:";
			this._labelDatasetInclusionCriteria.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelDatasetExclusionCriteria
			// 
			this._labelDatasetExclusionCriteria.AutoSize = true;
			this._labelDatasetExclusionCriteria.Location = new System.Drawing.Point(22, 99);
			this._labelDatasetExclusionCriteria.Name = "_labelDatasetExclusionCriteria";
			this._labelDatasetExclusionCriteria.Size = new System.Drawing.Size(128, 13);
			this._labelDatasetExclusionCriteria.TabIndex = 39;
			this._labelDatasetExclusionCriteria.Text = "Dataset exclusion criteria:";
			this._labelDatasetExclusionCriteria.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _checkboxIgnoreUnregisteredTables
			// 
			this._checkboxIgnoreUnregisteredTables.AutoSize = true;
			this._checkboxIgnoreUnregisteredTables.Location = new System.Drawing.Point(156, 170);
			this._checkboxIgnoreUnregisteredTables.Name = "_checkboxIgnoreUnregisteredTables";
			this._checkboxIgnoreUnregisteredTables.Size = new System.Drawing.Size(195, 17);
			this._checkboxIgnoreUnregisteredTables.TabIndex = 5;
			this._checkboxIgnoreUnregisteredTables.Text = "Ignore unregistered database tables";
			this._checkboxIgnoreUnregisteredTables.UseVisualStyleBackColor = true;
			// 
			// _tabPageAdvanced
			// 
			this._tabPageAdvanced.Controls.Add(this.label3);
			this._tabPageAdvanced.Controls.Add(this._comboBoxSqlCaseSensitivity);
			this._tabPageAdvanced.Controls.Add(this.groupBox3);
			this._tabPageAdvanced.Controls.Add(this.groupBox1);
			this._tabPageAdvanced.Controls.Add(this._numericUpDownDefaultMinimumSegmentLength);
			this._tabPageAdvanced.Controls.Add(this._labelSqlCaseSensitivity);
			this._tabPageAdvanced.Controls.Add(this._labelDefaultMinimumSegmentLength);
			this._tabPageAdvanced.Location = new System.Drawing.Point(4, 22);
			this._tabPageAdvanced.Name = "_tabPageAdvanced";
			this._tabPageAdvanced.Padding = new System.Windows.Forms.Padding(3);
			this._tabPageAdvanced.Size = new System.Drawing.Size(657, 274);
			this._tabPageAdvanced.TabIndex = 2;
			this._tabPageAdvanced.Text = "Advanced";
			this._tabPageAdvanced.UseVisualStyleBackColor = true;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(341, 17);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(260, 13);
			this.label3.TabIndex = 30;
			this.label3.Text = "used for SQL statements processed by the QA engine";
			// 
			// _comboBoxSqlCaseSensitivity
			// 
			this._comboBoxSqlCaseSensitivity.FormattingEnabled = true;
			this._comboBoxSqlCaseSensitivity.Location = new System.Drawing.Point(200, 14);
			this._comboBoxSqlCaseSensitivity.Name = "_comboBoxSqlCaseSensitivity";
			this._comboBoxSqlCaseSensitivity.Size = new System.Drawing.Size(135, 21);
			this._comboBoxSqlCaseSensitivity.TabIndex = 0;
			// 
			// groupBox3
			// 
			this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox3.Controls.Add(this._objectReferenceControlSchemaOwnerConnectionProvider);
			this.groupBox3.Controls.Add(this._objectReferenceControlRepositoryOwnerConnectionProvider);
			this.groupBox3.Controls.Add(this._labelRepositoryOwnerConnectionProvider);
			this.groupBox3.Controls.Add(this._labelSchemaOwnerConnectionProvider);
			this.groupBox3.Location = new System.Drawing.Point(8, 173);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(631, 90);
			this.groupBox3.TabIndex = 3;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Master database connections";
			// 
			// _objectReferenceControlSchemaOwnerConnectionProvider
			// 
			this._objectReferenceControlSchemaOwnerConnectionProvider.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._objectReferenceControlSchemaOwnerConnectionProvider.DataSource = null;
			this._objectReferenceControlSchemaOwnerConnectionProvider.DisplayMember = null;
			this._objectReferenceControlSchemaOwnerConnectionProvider.FindObjectDelegate = null;
			this._objectReferenceControlSchemaOwnerConnectionProvider.FormatTextDelegate = null;
			this._objectReferenceControlSchemaOwnerConnectionProvider.Location = new System.Drawing.Point(192, 25);
			this._objectReferenceControlSchemaOwnerConnectionProvider.Name = "_objectReferenceControlSchemaOwnerConnectionProvider";
			this._objectReferenceControlSchemaOwnerConnectionProvider.ReadOnly = false;
			this._objectReferenceControlSchemaOwnerConnectionProvider.Size = new System.Drawing.Size(414, 20);
			this._objectReferenceControlSchemaOwnerConnectionProvider.TabIndex = 0;
			// 
			// _objectReferenceControlRepositoryOwnerConnectionProvider
			// 
			this._objectReferenceControlRepositoryOwnerConnectionProvider.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._objectReferenceControlRepositoryOwnerConnectionProvider.DataSource = null;
			this._objectReferenceControlRepositoryOwnerConnectionProvider.DisplayMember = null;
			this._objectReferenceControlRepositoryOwnerConnectionProvider.FindObjectDelegate = null;
			this._objectReferenceControlRepositoryOwnerConnectionProvider.FormatTextDelegate = null;
			this._objectReferenceControlRepositoryOwnerConnectionProvider.Location = new System.Drawing.Point(192, 51);
			this._objectReferenceControlRepositoryOwnerConnectionProvider.Name = "_objectReferenceControlRepositoryOwnerConnectionProvider";
			this._objectReferenceControlRepositoryOwnerConnectionProvider.ReadOnly = false;
			this._objectReferenceControlRepositoryOwnerConnectionProvider.Size = new System.Drawing.Size(414, 20);
			this._objectReferenceControlRepositoryOwnerConnectionProvider.TabIndex = 1;
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this._labelAttributeConfiguratorFactoryClassDescriptor);
			this.groupBox1.Controls.Add(this._objectReferenceControlDatasetListBuilderFactoryClassDescriptor);
			this.groupBox1.Controls.Add(this._labelDatasetListBuilderFactoryClassDescriptor);
			this.groupBox1.Controls.Add(this._objectReferenceControlAttributeConfiguratorFactoryClassDescriptor);
			this.groupBox1.Location = new System.Drawing.Point(8, 67);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(631, 90);
			this.groupBox1.TabIndex = 2;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Harvesting components";
			// 
			// _objectReferenceControlDatasetListBuilderFactoryClassDescriptor
			// 
			this._objectReferenceControlDatasetListBuilderFactoryClassDescriptor.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._objectReferenceControlDatasetListBuilderFactoryClassDescriptor.DataSource = null;
			this._objectReferenceControlDatasetListBuilderFactoryClassDescriptor.DisplayMember = null;
			this._objectReferenceControlDatasetListBuilderFactoryClassDescriptor.FindObjectDelegate = null;
			this._objectReferenceControlDatasetListBuilderFactoryClassDescriptor.FormatTextDelegate = null;
			this._objectReferenceControlDatasetListBuilderFactoryClassDescriptor.Location = new System.Drawing.Point(192, 51);
			this._objectReferenceControlDatasetListBuilderFactoryClassDescriptor.Name = "_objectReferenceControlDatasetListBuilderFactoryClassDescriptor";
			this._objectReferenceControlDatasetListBuilderFactoryClassDescriptor.ReadOnly = false;
			this._objectReferenceControlDatasetListBuilderFactoryClassDescriptor.Size = new System.Drawing.Size(414, 20);
			this._objectReferenceControlDatasetListBuilderFactoryClassDescriptor.TabIndex = 1;
			// 
			// _objectReferenceControlAttributeConfiguratorFactoryClassDescriptor
			// 
			this._objectReferenceControlAttributeConfiguratorFactoryClassDescriptor.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._objectReferenceControlAttributeConfiguratorFactoryClassDescriptor.DataSource = null;
			this._objectReferenceControlAttributeConfiguratorFactoryClassDescriptor.DisplayMember = null;
			this._objectReferenceControlAttributeConfiguratorFactoryClassDescriptor.FindObjectDelegate = null;
			this._objectReferenceControlAttributeConfiguratorFactoryClassDescriptor.FormatTextDelegate = null;
			this._objectReferenceControlAttributeConfiguratorFactoryClassDescriptor.Location = new System.Drawing.Point(192, 25);
			this._objectReferenceControlAttributeConfiguratorFactoryClassDescriptor.Name = "_objectReferenceControlAttributeConfiguratorFactoryClassDescriptor";
			this._objectReferenceControlAttributeConfiguratorFactoryClassDescriptor.ReadOnly = false;
			this._objectReferenceControlAttributeConfiguratorFactoryClassDescriptor.Size = new System.Drawing.Size(414, 20);
			this._objectReferenceControlAttributeConfiguratorFactoryClassDescriptor.TabIndex = 0;
			// 
			// _labelSqlCaseSensitivity
			// 
			this._labelSqlCaseSensitivity.AutoSize = true;
			this._labelSqlCaseSensitivity.Location = new System.Drawing.Point(89, 17);
			this._labelSqlCaseSensitivity.Name = "_labelSqlCaseSensitivity";
			this._labelSqlCaseSensitivity.Size = new System.Drawing.Size(105, 13);
			this._labelSqlCaseSensitivity.TabIndex = 29;
			this._labelSqlCaseSensitivity.Text = "SQL case sensitivity:";
			this._labelSqlCaseSensitivity.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _objectReferenceControlSpatialReferenceDescriptor
			// 
			this._objectReferenceControlSpatialReferenceDescriptor.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._objectReferenceControlSpatialReferenceDescriptor.DataSource = null;
			this._objectReferenceControlSpatialReferenceDescriptor.DisplayMember = null;
			this._objectReferenceControlSpatialReferenceDescriptor.FindObjectDelegate = null;
			this._objectReferenceControlSpatialReferenceDescriptor.FormatTextDelegate = null;
			this._objectReferenceControlSpatialReferenceDescriptor.Location = new System.Drawing.Point(147, 100);
			this._objectReferenceControlSpatialReferenceDescriptor.Name = "_objectReferenceControlSpatialReferenceDescriptor";
			this._objectReferenceControlSpatialReferenceDescriptor.ReadOnly = false;
			this._objectReferenceControlSpatialReferenceDescriptor.Size = new System.Drawing.Size(505, 20);
			this._objectReferenceControlSpatialReferenceDescriptor.TabIndex = 2;
			// 
			// _buttonGoToSpatialReference
			// 
			this._buttonGoToSpatialReference.FlatAppearance.BorderSize = 0;
			this._buttonGoToSpatialReference.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this._buttonGoToSpatialReference.Image = global::ProSuite.DdxEditor.Framework.Properties.Resources.GoToItem;
			this._buttonGoToSpatialReference.Location = new System.Drawing.Point(125, 98);
			this._buttonGoToSpatialReference.Margin = new System.Windows.Forms.Padding(1);
			this._buttonGoToSpatialReference.Name = "_buttonGoToSpatialReference";
			this._buttonGoToSpatialReference.Size = new System.Drawing.Size(18, 22);
			this._buttonGoToSpatialReference.TabIndex = 40;
			this._buttonGoToSpatialReference.UseVisualStyleBackColor = true;
			this._buttonGoToSpatialReference.Click += new System.EventHandler(this._buttonGoToSpatialReference_Click);
			// 
			// _buttonGoToUserConnectionProvider
			// 
			this._buttonGoToUserConnectionProvider.FlatAppearance.BorderSize = 0;
			this._buttonGoToUserConnectionProvider.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this._buttonGoToUserConnectionProvider.Image = global::ProSuite.DdxEditor.Framework.Properties.Resources.GoToItem;
			this._buttonGoToUserConnectionProvider.Location = new System.Drawing.Point(122, 17);
			this._buttonGoToUserConnectionProvider.Margin = new System.Windows.Forms.Padding(1);
			this._buttonGoToUserConnectionProvider.Name = "_buttonGoToUserConnectionProvider";
			this._buttonGoToUserConnectionProvider.Size = new System.Drawing.Size(18, 22);
			this._buttonGoToUserConnectionProvider.TabIndex = 40;
			this._buttonGoToUserConnectionProvider.UseVisualStyleBackColor = true;
			this._buttonGoToUserConnectionProvider.Click += _buttonGoToUserConnectionProvider_Clicked;
			// 
			// ModelControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._buttonGoToSpatialReference);
			this.Controls.Add(this._tabControl);
			this.Controls.Add(this._textBoxDescription);
			this.Controls.Add(this._labelDescription);
			this.Controls.Add(this._objectReferenceControlSpatialReferenceDescriptor);
			this.Controls.Add(this._labelSpatialReferenceDescriptor);
			this.Controls.Add(this._textBoxName);
			this.Controls.Add(this._labelName);
			this.Name = "ModelControl";
			this.Size = new System.Drawing.Size(671, 429);
			((System.ComponentModel.ISupportInitialize)(this._numericUpDownDefaultMinimumSegmentLength)).EndInit();
			this._tabControl.ResumeLayout(false);
			this._tabPageDefaultDatabase.ResumeLayout(false);
			this._tabPageDefaultDatabase.PerformLayout();
			this.groupBox4.ResumeLayout(false);
			this.groupBox4.PerformLayout();
			this.tabPage2.ResumeLayout(false);
			this.tabPage2.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this._tabPageAdvanced.ResumeLayout(false);
			this._tabPageAdvanced.PerformLayout();
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _labelName;
        private System.Windows.Forms.TextBox _textBoxName;
        private System.Windows.Forms.Label _labelSpatialReferenceDescriptor;
        private ObjectReferenceControl _objectReferenceControlSpatialReferenceDescriptor;
        private System.Windows.Forms.Label _labelUserConnectionProvider;
        private System.Windows.Forms.Label _labelRepositoryOwnerConnectionProvider;
        private System.Windows.Forms.Label _labelSchemaOwnerConnectionProvider;
        private ObjectReferenceControl _objectReferenceControlUserConnectionProvider;
        private ObjectReferenceControl _objectReferenceControlRepositoryOwnerConnectionProvider;
        private ObjectReferenceControl _objectReferenceControlSchemaOwnerConnectionProvider;
        private System.Windows.Forms.TextBox _textBoxDescription;
		private System.Windows.Forms.Label _labelDescription;
        private System.Windows.Forms.TextBox _textBoxDatasetPrefix;
		private System.Windows.Forms.Label _labelDatasetPrefix;
        private System.Windows.Forms.Label _labelDefaultMinimumSegmentLength;
		private System.Windows.Forms.NumericUpDown _numericUpDownDefaultMinimumSegmentLength;
        private System.Windows.Forms.Label _labelAttributeConfiguratorFactoryClassDescriptor;
        private ObjectReferenceControl _objectReferenceControlAttributeConfiguratorFactoryClassDescriptor;
		private System.Windows.Forms.Label _labelDatasetListBuilderFactoryClassDescriptor;
		private ObjectReferenceControl _objectReferenceControlDatasetListBuilderFactoryClassDescriptor;
		private System.Windows.Forms.CheckBox _checkBoxIgnoreUnversionedDatasets;
		private System.Windows.Forms.CheckBox _checkBoxHarvestQualifiedElementNames;
		private System.Windows.Forms.CheckBox _checkBoxUpdateAliasNamesOnHarvest;
		private System.Windows.Forms.TabControl _tabControl;
		private System.Windows.Forms.TabPage _tabPageDefaultDatabase;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.TabPage _tabPageAdvanced;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.TextBox _textBoxDatasetInclusionCriteria;
		private System.Windows.Forms.Label _labelDatasetExclusionCriteria;
		private System.Windows.Forms.TextBox _textBoxDatasetExclusionCriteria;
		private System.Windows.Forms.Label _labelDatasetInclusionCriteria;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.TextBox _textBoxSchemaOwner;
		private System.Windows.Forms.Label _labelSchemaOwner;
		private System.Windows.Forms.TextBox _textBoxDefaultDatabaseSchemaOwner;
		private System.Windows.Forms.TextBox _textBoxDefaultDatabaseName;
		private System.Windows.Forms.Label _labelDefaultDatabaseSchemaOwner;
		private System.Windows.Forms.Label _labelDefaultDatabaseName;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox _textBoxLastHarvestedByUser;
		private System.Windows.Forms.Label _labelLastHarvestedByUser;
		private System.Windows.Forms.TextBox _textBoxLastHarvestedConnectionString;
		private System.Windows.Forms.TextBox _textBoxLastHarvestedDate;
		private System.Windows.Forms.Label _labelLastHarvestedConnectionString;
		private System.Windows.Forms.Label _labelLastHarvestedDate;
		private System.Windows.Forms.CheckBox _checkboxIgnoreUnregisteredTables;
		private System.Windows.Forms.Label _labelElementNameQualificationStatus;
		private System.Windows.Forms.Button _buttonHarvestingPreview;
		private System.Windows.Forms.Label _labelUseDefaultDatabaseForSchemaOnly;
		private BooleanCombobox _booleanComboboxUseDefaultDatabaseForSchemaOnly;
		private System.Windows.Forms.ComboBox _comboBoxSqlCaseSensitivity;
		private System.Windows.Forms.Label _labelSqlCaseSensitivity;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button _buttonGoToSpatialReference;
		private System.Windows.Forms.Button _buttonGoToUserConnectionProvider;
	}
}
