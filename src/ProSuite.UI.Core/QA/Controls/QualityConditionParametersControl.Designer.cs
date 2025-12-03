using TestParameterValue = ProSuite.DomainModel.Core.QA.TestParameterValue;

namespace ProSuite.UI.Core.QA.Controls
{
	partial class QualityConditionParametersControl
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
			this.components = new System.ComponentModel.Container();
			this._testParameterValueBindingSource = new System.Windows.Forms.BindingSource(this.components);
			this._propertyGrid = new System.Windows.Forms.PropertyGrid();
			((System.ComponentModel.ISupportInitialize)(this._testParameterValueBindingSource)).BeginInit();
			this.SuspendLayout();
			// 
			// _testParameterValueBindingSource
			// 
			this._testParameterValueBindingSource.DataSource = typeof(TestParameterValue);
			// 
			// _propertyGrid
			// 
			this._propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this._propertyGrid.Location = new System.Drawing.Point(0, 0);
			this._propertyGrid.Name = "_propertyGrid";
			this._propertyGrid.PropertySort = System.Windows.Forms.PropertySort.NoSort;
			this._propertyGrid.Size = new System.Drawing.Size(353, 242);
			this._propertyGrid.TabIndex = 1;
			this._propertyGrid.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this._propertyGrid_PropertyValueChanged);
			// 
			// QualityConditionParametersControl
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this._propertyGrid);
			this.Name = "QualityConditionParametersControl";
			this.Size = new System.Drawing.Size(353, 242);
			((System.ComponentModel.ISupportInitialize)(this._testParameterValueBindingSource)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.BindingSource _testParameterValueBindingSource;
		private System.Windows.Forms.PropertyGrid _propertyGrid;
	}
}
