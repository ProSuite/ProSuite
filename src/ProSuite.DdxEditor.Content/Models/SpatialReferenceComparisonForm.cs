using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Persistence.WinForms;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.DdxEditor.Content.Models
{
	public partial class SpatialReferenceComparisonForm : Form
	{
		private readonly Dictionary<int, ExpectedValue> _expectedValuesByColumnIndex;

		#region Constructors

		public SpatialReferenceComparisonForm(
			[NotNull] IEnumerable<ModelDatasetSpatialReferenceComparison> comparisons,
			[CanBeNull] SpatialReferenceProperties expectedSpatialReferenceProperties)
		{
			Assert.ArgumentNotNull(comparisons, nameof(comparisons));

			InitializeComponent();

			var formStateManager = new BasicFormStateManager(this);
			formStateManager.RestoreState();
			FormClosed += delegate { formStateManager.SaveState(); };

			var gridHandler =
				new BoundDataGridHandler<SpatialReferenceComparisonItem>(_dataGridView);
			gridHandler.BindTo(GetItems(comparisons));
			gridHandler.AddColumns(ColumnDescriptor.GetColumns<SpatialReferenceComparisonItem>());

			_expectedValuesByColumnIndex = GetExpectedValues(
				expectedSpatialReferenceProperties, _dataGridView.Columns);
		}

		[NotNull]
		private static Dictionary<int, ExpectedValue> GetExpectedValues(
			[CanBeNull] SpatialReferenceProperties expectedSpatialReferenceProperties,
			[NotNull] DataGridViewColumnCollection columns)
		{
			var result = new Dictionary<int, ExpectedValue>();

			if (expectedSpatialReferenceProperties == null)
			{
				return result;
			}

			for (var i = 0; i < columns.Count; i++)
			{
				DataGridViewColumn column = columns[i];

				ExpectedValue expectedValue = GetExpectedValue(column,
				                                               expectedSpatialReferenceProperties);
				if (expectedValue != null)
				{
					result.Add(i, expectedValue);
				}
			}

			return result;
		}

		[CanBeNull]
		private static ExpectedValue GetExpectedValue(
			[NotNull] DataGridViewColumn column,
			[NotNull] SpatialReferenceProperties expectedProperties)
		{
			string propertyName = column.DataPropertyName;

			if (propertyName == "CSName")
			{
				return new ExpectedValue(expectedProperties.CSName, false, true);
			}

			if (propertyName == "FactoryCode")
			{
				return new ExpectedValue(expectedProperties.FactoryCode, false, true);
			}

			if (propertyName == "VcsName")
			{
				return new ExpectedValue(expectedProperties.VcsName, true, true);
			}

			if (propertyName == "HighPrecision")
			{
				return new ExpectedValue(expectedProperties.IsHighPrecision, false);
			}

			// XY
			if (propertyName == "XyTolerance")
			{
				return new ExpectedValue(expectedProperties.XyTolerance);
			}

			if (propertyName == "XyResolution")
			{
				return new ExpectedValue(expectedProperties.XyResolution);
			}

			if (propertyName == "DomainXMin")
			{
				return new ExpectedValue(expectedProperties.DomainXMin);
			}

			if (propertyName == "DomainYMin")
			{
				return new ExpectedValue(expectedProperties.DomainYMin);
			}

			if (propertyName == "DomainXMax")
			{
				return new ExpectedValue(expectedProperties.DomainXMax);
			}

			if (propertyName == "DomainYMax")
			{
				return new ExpectedValue(expectedProperties.DomainYMax);
			}

			// Z
			if (propertyName == "ZTolerance")
			{
				return new ExpectedValue(expectedProperties.ZTolerance, true);
			}

			if (propertyName == "ZResolution")
			{
				return new ExpectedValue(expectedProperties.ZResolution, true);
			}

			if (propertyName == "DomainZMin")
			{
				return new ExpectedValue(expectedProperties.DomainZMin, true);
			}

			if (propertyName == "DomainZMax")
			{
				return new ExpectedValue(expectedProperties.DomainZMax, true);
			}

			// M 
			if (propertyName == "MTolerance")
			{
				return new ExpectedValue(expectedProperties.MTolerance, true);
			}

			if (propertyName == "MResolution")
			{
				return new ExpectedValue(expectedProperties.MResolution, true);
			}

			if (propertyName == "DomainMMin")
			{
				return new ExpectedValue(expectedProperties.DomainMMin, true);
			}

			if (propertyName == "DomainMMax")
			{
				return new ExpectedValue(expectedProperties.DomainMMax, true);
			}

			return null;
		}

		#endregion

		[NotNull]
		private static IList<SpatialReferenceComparisonItem> GetItems(
			[NotNull] IEnumerable<ModelDatasetSpatialReferenceComparison> comparisons)
		{
			var result = new List<SpatialReferenceComparisonItem>();

			foreach (ModelDatasetSpatialReferenceComparison comparison in comparisons)
			{
				result.Add(new SpatialReferenceComparisonItem(comparison));
			}

			result.Sort(CompareItems);

			return new SortableBindingList<SpatialReferenceComparisonItem>(result);
		}

		private static int CompareItems(SpatialReferenceComparisonItem c1,
		                                SpatialReferenceComparisonItem c2)
		{
			int categoryCompare = string.CompareOrdinal(c1.DatasetCategory, c2.DatasetCategory);

			if (categoryCompare == 0)
			{
				// categories are equal
				return string.CompareOrdinal(c1.Dataset, c2.Dataset);
			}

			return string.IsNullOrEmpty(c1.DatasetCategory) !=
			       string.IsNullOrEmpty(c2.DatasetCategory)
				       ? categoryCompare * -1
				       : categoryCompare;
		}

		[NotNull]
		private static string Format(double value)
		{
			CultureInfo culture = CultureInfo.CurrentCulture;
			string result = string.Format(culture, "{0:F99}", value).TrimEnd('0');

			if (result.Length == 0)
			{
				return result;
			}

			char lastCharacter = result[result.Length - 1];

			return char.IsPunctuation(lastCharacter)
				       ? result + "0"
				       : result;
		}

		private void _buttonClose_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void _buttonCopy_Click(object sender, EventArgs e)
		{
			object content = _dataGridView.GetClipboardContent();

			if (content == null)
			{
				SetStatus("Nothing to copy");
				return;
			}

			Clipboard.SetDataObject(content);

			int selectedRows = _dataGridView.SelectedRows.Count;

			SetStatus(selectedRows > 1
				          ? "{0} selected rows copied"
				          : "{0} selected row copied", selectedRows);
		}

		private void SetStatus([NotNull] string format, params object[] args)
		{
			_statusLabel.Text = string.Format(format, args);
		}

		private void _dataGridView_CellFormatting(object sender,
		                                          DataGridViewCellFormattingEventArgs e)
		{
			ExpectedValue expectedValue;
			if (_expectedValuesByColumnIndex.TryGetValue(e.ColumnIndex, out expectedValue))
			{
				if (e.Value != null || ! expectedValue.Optional)
				{
					if (! Equals(e.Value, expectedValue.Value))
					{
						e.CellStyle.BackColor = expectedValue.DifferenceIsError
							                        ? Color.Red
							                        : Color.Yellow;
					}
				}
			}

			if (e.Value is double value)
			{
				e.Value = Format(value);
				e.FormattingApplied = true;
			}
		}

		private class ExpectedValue
		{
			public ExpectedValue(object value) : this(value, false) { }

			public ExpectedValue(object value, bool optional) : this(value, optional, false) { }

			public ExpectedValue(object value, bool optional, bool differenceIsError)
			{
				Value = value;
				Optional = optional;
				DifferenceIsError = differenceIsError;
			}

			[CanBeNull]
			public object Value { get; private set; }

			public bool Optional { get; private set; }

			public bool DifferenceIsError { get; private set; }
		}

		private class SpatialReferenceComparisonItem
		{
			public SpatialReferenceComparisonItem(
				[NotNull] ModelDatasetSpatialReferenceComparison comparison)
			{
				Assert.ArgumentNotNull(comparison, nameof(comparison));

				DatasetCategory = comparison.Dataset.DatasetCategory == null
					                  ? string.Empty
					                  : comparison.Dataset.DatasetCategory.Name;

				Dataset = comparison.Dataset.DisplayName;

				SpatialReferenceProperties properties = comparison.SpatialReferenceProperties;

				if (properties != null)
				{
					XyTolerance = properties.XyTolerance;
					XyResolution = properties.XyResolution;
					DomainXMin = properties.DomainXMin;
					DomainYMin = properties.DomainYMin;
					DomainXMax = properties.DomainXMax;
					DomainYMax = properties.DomainYMax;

					ZTolerance = properties.ZTolerance;
					ZResolution = properties.ZResolution;
					DomainZMin = properties.DomainZMin;
					DomainZMax = properties.DomainZMax;

					MTolerance = properties.MTolerance;
					MResolution = properties.MResolution;
					DomainMMin = properties.DomainMMin;
					DomainMMax = properties.DomainMMax;

					CSName = properties.CSName;
					FactoryCode = properties.FactoryCode;
					HighPrecision = properties.IsHighPrecision;
					VcsName = properties.VcsName;
				}
			}

			[UsedImplicitly]
			[DisplayName(@"Category")]
			public string DatasetCategory { get; private set; }

			[UsedImplicitly]
			public string Dataset { get; private set; }

			[UsedImplicitly]
			[DisplayName(@"XY Tolerance")]
			public double? XyTolerance { get; private set; }

			[UsedImplicitly]
			[DisplayName(@"XY Resolution")]
			public double? XyResolution { get; private set; }

			[UsedImplicitly]
			[DisplayName(@"X Min")]
			public double? DomainXMin { get; private set; }

			[UsedImplicitly]
			[DisplayName(@"Y Min")]
			public double? DomainYMin { get; private set; }

			[UsedImplicitly]
			[DisplayName(@"X Max")]
			public double? DomainXMax { get; private set; }

			[UsedImplicitly]
			[DisplayName(@"Y Max")]
			public double? DomainYMax { get; private set; }

			[UsedImplicitly]
			[DisplayName(@"Z Tolerance")]
			public double? ZTolerance { get; private set; }

			[UsedImplicitly]
			[DisplayName(@"Z Resolution")]
			public double? ZResolution { get; private set; }

			[UsedImplicitly]
			[DisplayName(@"Z Min")]
			public double? DomainZMin { get; private set; }

			[UsedImplicitly]
			[DisplayName(@"Z Max")]
			public double? DomainZMax { get; private set; }

			[UsedImplicitly]
			[DisplayName(@"M Tolerance")]
			public double? MTolerance { get; private set; }

			[UsedImplicitly]
			[DisplayName(@"M Resolution")]
			public double? MResolution { get; private set; }

			[UsedImplicitly]
			[DisplayName(@"M Min")]
			public double? DomainMMin { get; private set; }

			[UsedImplicitly]
			[DisplayName(@"M Max")]
			public double? DomainMMax { get; private set; }

			[UsedImplicitly]
			[DisplayName(@"High Precision")]
			public bool? HighPrecision { get; private set; }

			[UsedImplicitly]
			[DisplayName(@"Coordinate System")]
			public string CSName { get; private set; }

			[UsedImplicitly]
			[DisplayName(@"ID")]
			public int FactoryCode { get; private set; }

			[UsedImplicitly]
			[DisplayName(@"Vertical Coordinate System")]
			public string VcsName { get; private set; }
		}
	}
}
