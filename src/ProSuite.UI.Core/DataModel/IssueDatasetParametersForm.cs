using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.Persistence.WinForms;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.Commons.Validation;

namespace ProSuite.UI.Core.DataModel
{
	public partial class IssueDatasetParametersForm : Form
	{
		private readonly IList<string> _configurationKeywords;
		private readonly Parameters _parameters;
		private readonly BoundDataGridHandler<DatasetItem> _gridHandler;
		private const string _defaultKeyword = "DEFAULTS";

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="IssueDatasetParametersForm"/> class.
		/// </summary>
		/// <param name="supportsPrivileges"></param>
		/// <param name="spatialReference">The spatial reference.</param>
		/// <param name="missingTableNames">The missing table names.</param>
		/// <param name="existingTables">The existing tables.</param>
		/// <param name="configKeywords"></param>
		public IssueDatasetParametersForm([NotNull] IList<string> configKeywords,
		                                  bool supportsPrivileges,
		                                  [NotNull] SpatialReferenceInfo spatialReference,
		                                  // ReSharper disable once ParameterTypeCanBeEnumerable.Local
		                                  [NotNull] ICollection<string> missingTableNames,
		                                  [NotNull] ICollection<DatasetItem> existingTables)
		{
			Assert.ArgumentNotNull(configKeywords, nameof(configKeywords));
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));
			Assert.ArgumentNotNull(missingTableNames, nameof(missingTableNames));
			Assert.ArgumentNotNull(existingTables, nameof(existingTables));

			InitializeComponent();

			var formStateManager = new BasicFormStateManager(this);
			formStateManager.RestoreState();
			FormClosed += delegate { formStateManager.SaveState(); };

			_configurationKeywords = configKeywords;

			_parameters = GetDefaultParameters(spatialReference);

			_parameters.FeatureDatasetName = GetDefaultFeatureDatasetName(existingTables);

			_gridHandler = new BoundDataGridHandler<DatasetItem>(_dataGridViewDatasets);
			_gridHandler.BindTo(GetDatasetItems(missingTableNames, existingTables));

			var binder = new ScreenBinder<Parameters>(
				new ErrorProviderValidationMonitor(_errorProvider));

			binder.Bind(m => m.GridSize1)
			      .To(_textBoxGrid1)
			      .WithLabel(_labelGrid1);

			binder.Bind(m => m.GridSize2)
			      .To(_textBoxGrid2)
			      .WithLabel(_labelGrid2);

			binder.Bind(m => m.GridSize3)
			      .To(_textBoxGrid3)
			      .WithLabel(_labelGrid3);

			binder.Bind(m => m.FeatureDatasetName)
			      .To(_textBoxFeatureDatasetName)
			      .WithLabel(_labelFeatureDatasetName);

			binder.BindToModel(_parameters);

			RenderSpatialReference(spatialReference);

			_groupBoxPrivileges.Enabled = supportsPrivileges;
		}

		private void RenderSpatialReference([NotNull] SpatialReferenceInfo spatialReference)
		{
			_textBoxSpatialReferenceName.Text = spatialReference.Name;

			if (! double.IsNaN(spatialReference.XYResolution))
			{
				_textBoxXYResolution.Text = Format(spatialReference.XYResolution);
			}

			if (! double.IsNaN(spatialReference.XYTolerance))
			{
				_textBoxXYTolerance.Text = Format(spatialReference.XYTolerance);
			}

			if (! double.IsNaN(spatialReference.ZResolution))
			{
				_textBoxZResolution.Text = Format(spatialReference.ZResolution);
			}

			if (! double.IsNaN(spatialReference.ZTolerance))
			{
				_textBoxZTolerance.Text = Format(spatialReference.ZTolerance);
			}
		}

		#endregion

		public double GridSize1 => _parameters.GridSize1;

		public double GridSize2 => _parameters.GridSize2;

		public double GridSize3 => _parameters.GridSize3;

		[NotNull]
		public IList<string> GetReaders()
		{
			return GetTokens(_textBoxReaders.Text);
		}

		[NotNull]
		public IList<string> GetWriters()
		{
			return GetTokens(_textBoxWriters.Text);
		}

		[CanBeNull]
		public string ConfigurationKeyword =>
			_comboBoxConfigurationKeyword.SelectedIndex >= 0
				? _configurationKeywords[_comboBoxConfigurationKeyword.SelectedIndex]
				: null;

		[CanBeNull]
		public string FeatureDatasetName => _parameters.FeatureDatasetName;

		#region Non-public

		[NotNull]
		private static string Format(double value)
		{
			return StringUtils.FormatPreservingDecimalPlaces(
				value, CultureInfo.CurrentCulture);
		}

		[CanBeNull]
		private static string GetDefaultFeatureDatasetName(
			[NotNull] IEnumerable<DatasetItem> existingTables)
		{
			Assert.ArgumentNotNull(existingTables, nameof(existingTables));

			foreach (DatasetItem existingTable in existingTables)
			{
				if (existingTable.FeatureDataset == null)
				{
					continue;
				}

				return ModelElementNameUtils.GetUnqualifiedName(existingTable.FeatureDataset);
			}

			return null;
		}

		[NotNull]
		private static IList<DatasetItem> GetDatasetItems(
			[NotNull] IEnumerable<string> missingTableNames,
			[NotNull] IEnumerable<DatasetItem> existingTables)
		{
			var result = new List<DatasetItem>();

			foreach (string missingTableName in missingTableNames)
			{
				result.Add(new DatasetItem(missingTableName, false, null));
			}

			result.AddRange(existingTables);

			result.Sort(
				(d1, d2) => string.Compare(d1.DatasetName,
				                           d2.DatasetName,
				                           StringComparison.CurrentCulture));

			return result;
		}

		[NotNull]
		private static Parameters GetDefaultParameters(
			[NotNull] SpatialReferenceInfo spatialReference)
		{
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));

			return spatialReference.IsGeographicCoordinateSystem
				       ? new Parameters { GridSize1 = 1, GridSize2 = 50, GridSize3 = 0 }
				       : new Parameters { GridSize1 = 1000, GridSize2 = 50000, GridSize3 = 0 };
		}

		[NotNull]
		private static IList<string> GetTokens([CanBeNull] string text)
		{
			return text?.Split(";, ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries) ??
			       (IList<string>) new List<string>();
		}

		private void SelectKeyword([NotNull] string keyword)
		{
			foreach (object item in _comboBoxConfigurationKeyword.Items)
			{
				if (item == null)
				{
					continue;
				}

				if (! string.Equals(keyword,
				                    item.ToString(),
				                    StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				_msg.VerboseDebug(() => $"Selecting configuration keyword: {item}");

				_comboBoxConfigurationKeyword.SelectedItem = item;
				break;
			}
		}

		#region Event handlers

		private void IssueDatasetParametersForm_Load(object sender, EventArgs e)
		{
			_comboBoxConfigurationKeyword.BeginUpdate();

			try
			{
				_comboBoxConfigurationKeyword.DataSource = _configurationKeywords;

				SelectKeyword(_defaultKeyword);
			}
			finally
			{
				_comboBoxConfigurationKeyword.EndUpdate();
			}

			_dataGridViewDatasets.ClearSelection();
		}

		private void _buttonOK_Click(object sender, EventArgs e)
		{
			Notification notification = Validator.ValidateObject(_parameters);

			if (! notification.IsValid())
			{
				Dialog.WarningFormat(this, Text,
				                     "Cannot create issue datasets using the specified parameters:{0}{0}{1}",
				                     Environment.NewLine,
				                     ValidationUtils.FormatNotification(notification));
				return;
			}

			DialogResult = DialogResult.OK;

			Close();
		}

		private void _dataGridViewDatasets_CellFormatting(object sender,
		                                                  DataGridViewCellFormattingEventArgs
			                                                  e)
		{
			DatasetItem item = _gridHandler.GetRow(e.RowIndex);

			if (item == null)
			{
				return;
			}

			if (item.Exists)
			{
				e.CellStyle.BackColor = Color.FromArgb(240, 240, 240);
			}
			else
			{
				e.CellStyle.BackColor = Color.FromArgb(255, 255, 180);
				e.CellStyle.SelectionBackColor = Color.FromArgb(200, 200, 100);
			}
		}

		#endregion

		#endregion

		#region Nested types

		public class DatasetItem
		{
			private readonly bool _exists;
			private readonly string _datasetName;
			private readonly string _featureDataset;
			private readonly string _status;

			public DatasetItem([NotNull] string datasetName, bool exists,
			                   [CanBeNull] string featureDataset)
			{
				_exists = exists;
				_featureDataset = featureDataset;
				_datasetName = datasetName;
				_status = _exists
					          ? "Exists"
					          : "To be created";
			}

			[UsedImplicitly]
			public string DatasetName => _datasetName;

			[CanBeNull]
			[UsedImplicitly]
			public string FeatureDataset => _featureDataset;

			public bool Exists => _exists;

			[UsedImplicitly]
			public string Status => _status;
		}

		private class Parameters : IValidated
		{
			[GreaterOrEqualToZero]
			public double GridSize1 { get; set; }

			[GreaterOrEqualToZero]
			public double GridSize2 { get; set; }

			[GreaterOrEqualToZero]
			public double GridSize3 { get; set; }

			[UsedImplicitly]
			[MaximumStringLength(30)]
			[ValidToken(Optional = true, TrimBeforeValidation = true)]
			public string FeatureDatasetName { get; set; }

			public void Validate(Notification notification)
			{
				if (Math.Abs(GridSize1) < double.Epsilon &&
				    (GridSize2 > 0 || GridSize3 > 0))
				{
					notification.RegisterMessage("GridSize1",
					                             "Grid level 1 may not be undefined if higher grid level is defined",
					                             Severity.Error);
				}

				if (Math.Abs(GridSize2) < double.Epsilon && GridSize3 > 0)
				{
					notification.RegisterMessage("GridSize2",
					                             "Grid level 2 may not be undefined if grid level 3 is defined",
					                             Severity.Error);
				}

				if (GridSize2 > 0 && GridSize2 <= GridSize1)
				{
					notification.RegisterMessage("GridSize2",
					                             "Grid level 2 must be larger than grid level 1 (or 0)",
					                             Severity.Error);
				}

				if (GridSize3 > 0 && GridSize3 <= GridSize2)
				{
					notification.RegisterMessage("GridSize3",
					                             "Grid level 3 must be larger than grid level 2 (or 0)",
					                             Severity.Error);
				}
			}
		}

		#endregion
	}
}
