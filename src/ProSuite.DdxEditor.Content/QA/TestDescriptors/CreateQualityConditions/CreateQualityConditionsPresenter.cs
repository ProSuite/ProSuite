using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Misc;
using ProSuite.Commons.Text;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.DdxEditor.Content.Datasets;
using ProSuite.DdxEditor.Content.QA.QSpec;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.UI.Core.DataModel.ResourceLookup;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors.CreateQualityConditions
{
	internal class CreateQualityConditionsPresenter : ICreateQualityConditionsObserver,
	                                                  ICreateQualityConditionsFillDown
	{
		private const string _datasetColumnName = "#Dataset";
		private const string _datasetNameColumnName = "#DatasetName";
		private const string _filterExpressionColumnName = "#FilterExpression";
		private const string _filterExpressionColumnCaption = "Filter Expression";
		private const string _modelNameColumnName = "#ModelName";
		private const string _modelNameColumnCaption = "Model";
		private const string _categoryColumnName = "#DatasetCategory";
		private const string _categoryColumnCaption = "Category";
		private const string _imageColumnCaption = "Image";
		private const string _imageColumnName = "#Image";
		private const string _qualityConditionNameColumnCaption = "Quality Condition Name";
		private const string _qualityConditionNameColumnName = "#QualityConditionName";

		private const string _tokenAbbreviation = "{ABBREVIATION}";
		private const string _tokenAlias = "{ALIAS}";
		private const string _tokenName = "{NAME}";
		private const string _tokenUnqualifiedName = "{UNQUALIFIED_NAME}";
		private const string _tokenCategoryName = "{CATEGORY_NAME}";
		private const string _tokenCategoryAbbreviation = "{CATEGORY_ABBREVIATION}";
		private const string _tokenDatasetCategoryName = "{DATASET_CATEGORY_NAME}";

		private const string _tokenDatasetCategoryAbbreviation =
			"{DATASET_CATEGORY_ABBREVIATION}";

		private const string _tokenDataQualityCategoryName = "{QUALITY_CATEGORY_NAME}";

		private const string _tokenDataQualityCategoryAbbreviation =
			"{QUALITY_CATEGORY_ABBREVIATION}";

		private readonly string _datasetParameterName;
		private readonly DataTable _parametersDataTable;
		private readonly Latch _renderingLatch = new Latch();

		private readonly HashSet<string> _scalarParameterNames =
			new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		private readonly IDictionary<TestParameter, DataColumn> _scalarParameterColumns;

		private readonly TestDescriptorItem _testDescriptorItem;
		private readonly IList<TestParameter> _testParameters;

		private readonly SortableBindingList<QualitySpecificationTableRow>
			_selectedQualitySpecifications =
				new SortableBindingList<QualitySpecificationTableRow>();

		private readonly ICreateQualityConditionsView _view;
		private bool _hasUnappliedNamingConventionChange;
		private readonly ICollection<string> _existingQualityConditionNames;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="CreateQualityConditionsPresenter"/> class.
		/// </summary>
		/// <param name="view">The view.</param>
		/// <param name="testDescriptorItem">The test descriptor.</param>
		/// <param name="datasetParameterName">Name of the dataset parameter.</param>
		/// <param name="testParameters">The test parameters.</param>
		public CreateQualityConditionsPresenter(
			[NotNull] ICreateQualityConditionsView view,
			[NotNull] TestDescriptorItem testDescriptorItem,
			[NotNull] string datasetParameterName,
			[NotNull] IList<TestParameter> testParameters)
		{
			Assert.ArgumentNotNull(view, nameof(view));
			Assert.ArgumentNotNull(testDescriptorItem, nameof(testDescriptorItem));
			Assert.ArgumentNotNull(datasetParameterName, nameof(datasetParameterName));
			Assert.ArgumentNotNull(testParameters, nameof(testParameters));

			_view = view;
			_testDescriptorItem = testDescriptorItem;
			_datasetParameterName = datasetParameterName;
			_testParameters = testParameters;

			_view.TestDescriptorName = testDescriptorItem.Text;

			_view.QualityConditionNames = string.Format("{0}_{1}", _tokenAlias,
			                                            testDescriptorItem.Text);

			_existingQualityConditionNames = testDescriptorItem.GetQualityConditionNames();

			_parametersDataTable = CreateParametersDataTable(testParameters,
			                                                 datasetParameterName,
			                                                 out _scalarParameterColumns);

			_view.SupportedVariablesText = GetSupportedVariables();
			_view.Observer = this;

			UpdateParametersRowTools();
		}

		#endregion

		#region ICreateQualityConditionsObserver Members

		void ICreateQualityConditionsObserver.ViewLoaded()
		{
			_view.BindToQualitySpecifications(_selectedQualitySpecifications);
			UpdateQualitySpecificationTools();
		}

		void ICreateQualityConditionsObserver.OKClicked()
		{
			List<string> errorMessages;
			if (! IsValidForOK(out errorMessages))
			{
				var sb = new StringBuilder();
				sb.AppendLine("Unable to create quality conditions:");
				sb.AppendLine();
				foreach (string message in errorMessages)
				{
					sb.AppendFormat("- {0}", message);
					sb.AppendLine();
				}

				_view.Warn(sb.ToString());
				return;
			}

			int rowCount = _parametersDataTable.Rows.Count;

			string confirmMessage =
				string.Format(
					rowCount == 1
						? "Are you sure to create {0} quality condition using the names in column '{1}'?"
						: "Are you sure to create {0} quality conditions using the names in column '{1}'?",
					rowCount, _qualityConditionNameColumnCaption);

			if (_hasUnappliedNamingConventionChange)
			{
				const bool defaultIsCancel = true;
				if (! _view.Confirm(
					    string.Format(
						    "The current naming convention has not been applied to any quality condition.{0}{0}{1}",
						    Environment.NewLine,
						    confirmMessage),
					    defaultIsCancel))
				{
					return;
				}
			}
			else
			{
				const bool defaultIsCancel = false;
				if (! _view.Confirm(confirmMessage, defaultIsCancel))
				{
					return;
				}
			}

			_view.QualityConditionParameters =
				CreateQualityConditionParameters(_parametersDataTable);

			_view.DialogResult = DialogResult.OK;

			_view.Close();
		}

		void ICreateQualityConditionsObserver.CellValidated(DataRow dataRow,
		                                                    string columnName)
		{
			ValidateRow(dataRow, columnName);
		}

		void ICreateQualityConditionsObserver.CancelClicked()
		{
			_view.QualityConditionParameters = null;

			_view.DialogResult = DialogResult.Cancel;

			_view.Close();
		}

		void ICreateQualityConditionsObserver.AddClicked()
		{
			IList<DatasetTableRow> selectedDatasets =
				_testDescriptorItem.GetApplicableDatasets(_view, _datasetParameterName,
				                                          GetSelectedDatasets(),
				                                          _view.ExcludeDatasetsUsingThisTest,
				                                          _view.TargetCategory);

			if (selectedDatasets == null)
			{
				return; // cancelled
			}

			string naming = _view.QualityConditionNames;

			foreach (DatasetTableRow datasetTableRow in selectedDatasets)
			{
				AddDataRow(datasetTableRow.Dataset, _view.TargetCategory, naming);
			}

			_renderingLatch.RunInsideLatch(() => _view.BindParameters(_parametersDataTable));

			_hasUnappliedNamingConventionChange = false;

			UpdateParametersRowTools();
		}

		void ICreateQualityConditionsObserver.RemoveClicked()
		{
			_renderingLatch.RunInsideLatch(
				delegate
				{
					foreach (DataRow row in _view.SelectedParametersRows)
					{
						_parametersDataTable.Rows.Remove(row);
					}
				});

			UpdateParametersRowTools();
		}

		void ICreateQualityConditionsObserver.QualitySpecificationNamingChanged()
		{
			_hasUnappliedNamingConventionChange = true;
		}

		void ICreateQualityConditionsObserver.QualityConditionParametersSelectionChanged()
		{
			if (_renderingLatch.IsLatched)
			{
				return;
			}

			UpdateParametersRowTools();
		}

		void ICreateQualityConditionsObserver.CollectContextCommands(
			IList<ICommand> commands)
		{
			CellSelection cellSelection = _view.GetParametersCellSelection();

			if (cellSelection.CellCount > 0 &&
			    cellSelection.IsRectangular() &&
			    IsScalarParameterSelection(cellSelection))
			{
				commands.Add(new FillDownCommand(cellSelection, this));
			}
		}

		void ICreateQualityConditionsObserver.SelectAllClicked()
		{
			_renderingLatch.RunInsideLatch(_view.SelectAllParametersRows);
			UpdateParametersRowTools();
		}

		void ICreateQualityConditionsObserver.SelectNoneClicked()
		{
			_renderingLatch.RunInsideLatch(_view.ClearParametersRowSelection);
			UpdateParametersRowTools();
		}

		void ICreateQualityConditionsObserver.ApplyToSelectionClicked()
		{
			string naming = _view.QualityConditionNames;

			foreach (DataRow row in _view.SelectedParametersRows)
			{
				Dataset dataset = GetDataset(row);

				SetQualityConditionName(row, dataset, _view.TargetCategory, naming);
			}

			_hasUnappliedNamingConventionChange = false;
		}

		void ICreateQualityConditionsObserver.AssignToQualitySpecificationsClicked()
		{
			var qualitySpecifications = new HashSet<QualitySpecification>();
			foreach (QualitySpecificationTableRow tableRow in _selectedQualitySpecifications)
			{
				qualitySpecifications.Add(tableRow.QualitySpecification);
			}

			IList<QualitySpecificationTableRow> tableRows =
				_testDescriptorItem.GetQualitySpecificationsToReference(
					_view, qualitySpecifications, _view.TargetCategory);

			if (tableRows == null)
			{
				return;
			}

			foreach (QualitySpecificationTableRow tableRow in tableRows)
			{
				Assert.False(qualitySpecifications.Contains(
					             tableRow.QualitySpecification),
				             "quality specification already in selection");
				qualitySpecifications.Add(tableRow.QualitySpecification);

				_selectedQualitySpecifications.Add(tableRow);
			}
		}

		void ICreateQualityConditionsObserver.RemoveFromQualitySpecificationsClicked()
		{
			// get selected targets
			IList<QualitySpecificationTableRow> selected =
				_view.GetSelectedQualitySpecifications();

			// remove them from the entity
			foreach (QualitySpecificationTableRow tableRow in selected)
			{
				_selectedQualitySpecifications.Remove(tableRow);
			}
		}

		void ICreateQualityConditionsObserver.QualitySpecificationSelectionChanged()
		{
			UpdateQualitySpecificationTools();
		}

		bool ICreateQualityConditionsObserver.CanFindCategory =>
			_testDescriptorItem.CanFindCategory;

		object ICreateQualityConditionsObserver.FindCategory()
		{
			DataQualityCategory category;
			return _testDescriptorItem.FindCategory(_view, out category)
				       ? category
				       : null;
		}

		string ICreateQualityConditionsObserver.FormatCategoryText(object value)
		{
			if (value == null)
			{
				return "<no category>";
			}

			var category = value as DataQualityCategory;

			Assert.NotNull(category, "Unexpected value: {0}", value);

			return category.GetQualifiedName();
		}

		#endregion

		#region ICreateQualityConditionsFillDown Members

		void ICreateQualityConditionsFillDown.FillDown(CellSelection cellSelection)
		{
			foreach (string columnName in cellSelection.GetColumnNames())
			{
				IList<DataRow> dataRows = cellSelection.GetRows(columnName);
				Assert.True(dataRows.Count > 0, "no data rows");

				object firstValue = dataRows[0][columnName];

				for (var rowIndex = 1; rowIndex < dataRows.Count; rowIndex++)
				{
					DataRow row = dataRows[rowIndex];
					row[columnName] = firstValue;

					ValidateRow(row, columnName);
				}
			}
		}

		#endregion

		private void AddDataRow([NotNull] IDdxDataset dataset,
		                        [CanBeNull] DataQualityCategory targetCategory,
		                        [NotNull] string naming)
		{
			DataRow row = _parametersDataTable.Rows.Add();

			DatasetCategory category = dataset.DatasetCategory;

			row[_datasetColumnName] = dataset;
			row[_imageColumnName] = DatasetTypeImageLookup.GetImage(dataset);
			row[_datasetNameColumnName] = dataset.AliasName;
			row[_categoryColumnName] = category == null
				                           ? string.Empty
				                           : category.Name;
			row[_modelNameColumnName] = dataset.Model.Name;
			row[_filterExpressionColumnName] = null;

			SetQualityConditionName(row, dataset, targetCategory, naming);

			// TODO set default values for all optional parameters
			foreach (TestParameter parameter in _testParameters)
			{
				DataColumn dataColumn;
				if (parameter.DefaultValue != null &&
				    _scalarParameterColumns.TryGetValue(parameter, out dataColumn))
				{
					row[dataColumn] = parameter.DefaultValue;
				}
			}
		}

		private void ValidateRow([NotNull] DataRow dataRow, [NotNull] string columnName)
		{
			string errorMessage;
			dataRow.SetColumnError(columnName,
			                       IsValid(dataRow, columnName, out errorMessage)
				                       ? string.Empty
				                       : errorMessage);
		}

		private static void SetQualityConditionName(
			[NotNull] DataRow row,
			[NotNull] IDdxDataset dataset,
			[CanBeNull] DataQualityCategory dataQualityCategory,
			[NotNull] string naming)
		{
			const string columnName = _qualityConditionNameColumnName;

			string name;
			List<string> errorMessages;
			if (TryCreateName(dataset, dataQualityCategory, naming,
			                  out name, out errorMessages))
			{
				row[columnName] = name;
				row.SetColumnError(columnName, string.Empty);
				return;
			}

			_msg.WarnFormat("Unable to generate quality condition name for dataset '{0}'",
			                dataset.AliasName);
			using (_msg.IncrementIndentation())
			{
				foreach (string errorMessage in errorMessages)
				{
					_msg.Warn(errorMessage);
				}
			}

			row[columnName] = string.Empty;
			row.SetColumnError(columnName,
			                   StringUtils.Concatenate(errorMessages,
			                                           Environment.NewLine));
		}

		private static string GetSupportedVariables()
		{
			return string.Join(" ", new[]
			                        {
				                        _tokenName,
				                        _tokenAlias,
				                        _tokenAbbreviation,
				                        _tokenUnqualifiedName,
				                        _tokenDatasetCategoryName,
				                        _tokenDatasetCategoryAbbreviation,
				                        _tokenDataQualityCategoryName,
				                        _tokenDataQualityCategoryAbbreviation
			                        });
		}

		[NotNull]
		private static IEnumerable<KeyValuePair<string, string>> GetNameValuePairs(
			[NotNull] IDdxDataset dataset,
			[CanBeNull] DataQualityCategory dataQualityCategory)
		{
			DatasetCategory datasetCategory = dataset.DatasetCategory;

			yield return new KeyValuePair<string, string>(_tokenName, dataset.Name);

			yield return new KeyValuePair<string, string>(_tokenAlias, dataset.AliasName);

			yield return new KeyValuePair<string, string>(_tokenAbbreviation,
			                                              dataset.Abbreviation);

			yield return new KeyValuePair<string, string>(_tokenUnqualifiedName,
			                                              dataset.UnqualifiedName);

			yield return new KeyValuePair<string, string>(_tokenDatasetCategoryName,
			                                              datasetCategory != null
				                                              ? datasetCategory.Name
				                                              : string.Empty);

			yield return new KeyValuePair<string, string>(_tokenDatasetCategoryAbbreviation,
			                                              datasetCategory != null
				                                              ? datasetCategory.Abbreviation
				                                              : string.Empty);

			yield return new KeyValuePair<string, string>(_tokenDataQualityCategoryName,
			                                              dataQualityCategory != null
				                                              ? dataQualityCategory.Name
				                                              : string.Empty);

			yield return
				new KeyValuePair<string, string>(_tokenDataQualityCategoryAbbreviation,
				                                 dataQualityCategory != null
					                                 ? dataQualityCategory.Abbreviation
					                                 : string.Empty);

			// old names for dataset category still supported for compatibility
			yield return new KeyValuePair<string, string>(_tokenCategoryName,
			                                              datasetCategory != null
				                                              ? datasetCategory.Name
				                                              : string.Empty);

			yield return new KeyValuePair<string, string>(_tokenCategoryAbbreviation,
			                                              datasetCategory != null
				                                              ? datasetCategory.Abbreviation
				                                              : string.Empty);
		}

		private bool IsValidForOK([NotNull] out List<string> errorMessages)
		{
			errorMessages = new List<string>();

			var columnNames = new List<string>(_scalarParameterNames)
			                  {_qualityConditionNameColumnName};

			foreach (DataRow row in _parametersDataTable.Rows)
			{
				foreach (string columnName in columnNames)
				{
					string errorMessage;
					if (! IsValid(row, columnName, out errorMessage))
					{
						errorMessages.Add(errorMessage);

						row.SetColumnError(columnName, errorMessage);
					}
					else
					{
						row.SetColumnError(columnName, string.Empty);
					}
				}
			}

			ValidateQualityConditionNames(errorMessages);

			return errorMessages.Count == 0;
		}

		private void ValidateQualityConditionNames(
			[NotNull] ICollection<string> errorMessages)
		{
			var countByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			const string columnName = _qualityConditionNameColumnName;

			foreach (DataRow row in _parametersDataTable.Rows)
			{
				var name = (string) row[columnName];

				int count;
				if (! countByName.TryGetValue(name, out count))
				{
					count = 0;
				}

				countByName[name] = count + 1;
			}

			foreach (DataRow row in _parametersDataTable.Rows)
			{
				var name = (string) row[columnName];

				string errorMessage = null;
				if (_existingQualityConditionNames.Contains(name))
				{
					errorMessage = string.Format("There exists a quality condition with name '{0}'",
					                             name);
				}
				else if (countByName[name] > 1)
				{
					errorMessage = string.Format(
						"The quality condition name '{0}' is not unique within the list",
						name);
				}

				if (errorMessage != null)
				{
					row.SetColumnError(columnName, errorMessage);
					errorMessages.Add(errorMessage);
				}
				else
				{
					row.SetColumnError(columnName, string.Empty);
				}
			}
		}

		private bool IsValid([NotNull] DataRow parametersRow,
		                     [NotNull] string columnName,
		                     [NotNull] out string errorMessage)
		{
			object value = parametersRow[columnName];

			if (IsNullOrEmpty(value) && ! AllowNullValue(parametersRow, columnName))
			{
				errorMessage = string.Format("No value for {0} ({1})",
				                             GetCaption(parametersRow, columnName),
				                             GetDataset(parametersRow).AliasName);
				return false;
			}

			errorMessage = string.Empty;
			return true;
		}

		[NotNull]
		private static string GetCaption([NotNull] DataRow row,
		                                 [NotNull] string columnName)
		{
			return row.Table.Columns[columnName].Caption;
		}

		private bool AllowNullValue([NotNull] DataRow row,
		                            [NotNull] string columnName)
		{
			if (Equals(columnName, _qualityConditionNameColumnName))
			{
				// the quality condition column never allows null values
				return false;
			}

			if (! _scalarParameterNames.Contains(columnName))
			{
				// it's not one of the editable columns, allow null values
				return true;
			}

			// it's a scalar parameter
			if (row.Table.Columns[columnName].DataType.IsValueType)
			{
				// can't accept null for value type parameter
				return false;
			}

			// it's a reference type parameter; accept null
			// TODO evaluate [NotNull] attribute on test parameter
			// TODO and/or validate parameters by instantiating the test!
			return true;
		}

		private static bool IsNullOrEmpty([CanBeNull] object value)
		{
			if (value == DBNull.Value || value == null)
			{
				return true;
			}

			var stringValue = value as string;
			return stringValue != null && StringUtils.IsNullOrEmptyOrBlank(stringValue);
		}

		private bool IsScalarParameterSelection([NotNull] CellSelection cellSelection)
		{
			return cellSelection.GetColumnNames().All(
				columnName => _scalarParameterNames.Contains(columnName));
		}

		private void UpdateParametersRowTools()
		{
			bool hasSelection = _view.SelectedParametersRowCount > 0;
			bool hasAnyRows = _view.TotalParametersRowCount > 0;

			_view.SelectAllParametersRowsEnabled = hasAnyRows;

			_view.ClearParametersRowSelectionEnabled = hasSelection;
			_view.ApplyToParametersRowSelectionEnabled = hasSelection;
			_view.RemoveSelectedParametersRowsEnabled = hasSelection;
			_view.OKEnabled = hasAnyRows;
		}

		private void UpdateQualitySpecificationTools()
		{
			_view.RemoveFromQualitySpecificationsEnabled =
				_view.HasSelectedQualitySpecifications;
		}

		[NotNull]
		private IList<QualityConditionParameters> CreateQualityConditionParameters(
			[NotNull] DataTable parametersDataTable)
		{
			var result = new List<QualityConditionParameters>();

			foreach (DataRow row in parametersDataTable.Rows)
			{
				Dataset dataset = GetDataset(row);
				var qualityConditionName = (string) row[_qualityConditionNameColumnName];
				var filterExpression = row[_filterExpressionColumnName] as string;
				// can be DBNull -> null

				var parameters = new QualityConditionParameters(dataset,
				                                                qualityConditionName,
				                                                filterExpression);

				foreach (TestParameter testParameter in _testParameters)
				{
					if (! TestParameterTypeUtils.IsDatasetType(testParameter.Type))
					{
						DataColumn dataColumn;
						if (_scalarParameterColumns.TryGetValue(testParameter, out dataColumn))
						{
							parameters.AddScalarParameter(testParameter.Name, row[dataColumn]);
						}
					}
				}

				foreach (QualitySpecificationTableRow tableRow in _selectedQualitySpecifications)
				{
					parameters.AddQualitySpecification(tableRow.QualitySpecification);
				}

				result.Add(parameters);
			}

			return result;
		}

		[NotNull]
		private static Dataset GetDataset([NotNull] DataRow row)
		{
			return (Dataset) row[_datasetColumnName];
		}

		[NotNull]
		private static IList<TestParameter> GetEditableScalarParameters(
			[NotNull] IEnumerable<TestParameter> testParameters,
			[NotNull] string datasetParameterName)
		{
			var result = new List<TestParameter>();

			foreach (TestParameter testParameter in testParameters)
			{
				if (testParameter.ArrayDimension > 0)
				{
					if (! testParameter.IsConstructorParameter)
					{
						// ignore *optional* parameters with array dimension > 0
						continue;
					}

					// fail for constructor parameters
					Assert.Fail("Unexpected array dimension: {0}",
					            testParameter.ArrayDimension);
				}

				if (TestParameterTypeUtils.IsDatasetType(testParameter.Type))
				{
					Assert.AreEqual(testParameter.Name, datasetParameterName,
					                "Unexpected dataset parameter name");
					continue;
				}

				// it's a scalar parameter
				result.Add(testParameter);
			}

			return result;
		}

		[NotNull]
		private DataTable CreateParametersDataTable(
			[NotNull] IEnumerable<TestParameter> testParameters,
			[NotNull] string datasetParameterName,
			[NotNull] out IDictionary<TestParameter, DataColumn> scalarParameterColumns)
		{
			var result = new DataTable("testParameters");

			IList<TestParameter> scalarParameters = GetEditableScalarParameters(
				testParameters, datasetParameterName);

			DataColumnCollection columns = result.Columns;

			columns.Add(CreateImageColumn());
			columns.Add(CreateDatasetColumn());
			columns.Add(CreateDatasetNameColumn(datasetParameterName));
			columns.Add(CreateCategoryNameColumn());
			columns.Add(CreateModelNameColumn());
			columns.Add(CreateFilterExpressionColumn());

			bool fillQualityConditionNameColumn = scalarParameters.Count == 0;
			columns.Add(CreateQualityConditionNameColumn(fillQualityConditionNameColumn));

			scalarParameterColumns = new Dictionary<TestParameter, DataColumn>();

			foreach (TestParameter testParameter in scalarParameters)
			{
				// it's a scalar parameter

				DataColumn dataColumn = CreateScalarParameterColumn(testParameter);
				columns.Add(dataColumn);

				_scalarParameterNames.Add(testParameter.Name);

				scalarParameterColumns.Add(testParameter, dataColumn);
			}

			return result;
		}

		[NotNull]
		private DataColumn CreateScalarParameterColumn(
			[NotNull] TestParameter testParameter)
		{
			Assert.ArgumentNotNull(testParameter, nameof(testParameter));

			// the scalar parameter will expect string input
			var column = new DataColumn(GetColumnName(testParameter), testParameter.Type)
			             {
				             Caption = GetColumnCaption(testParameter),
				             ReadOnly = false
			             };

			DataGridViewColumn gridColumn = CreateGridColumn(testParameter, column);

			_view.AddParametersColumn(gridColumn);

			return column;
		}

		[NotNull]
		private static string GetColumnCaption([NotNull] TestParameter testParameter)
		{
			return testParameter.IsConstructorParameter
				       ? testParameter.Name
				       : string.Format("[{0}]", testParameter.Name);
		}

		[NotNull]
		private static string GetColumnName([NotNull] TestParameter testParameter)
		{
			return testParameter.Name;
		}

		[NotNull]
		private static DataGridViewColumn CreateGridColumn(
			[NotNull] TestParameter testParameter,
			[NotNull] DataColumn dataColumn)
		{
			Assert.ArgumentNotNull(testParameter, nameof(testParameter));

			DataGridViewColumn result;
			if (testParameter.Type.IsEnum)
			{
				result = CreateEnumComboboxColumn(testParameter);
			}
			else if (testParameter.Type == typeof(bool))
			{
				result = CreateBooleanComboboxColumn();
			}
			else
			{
				result = new DataGridViewTextBoxColumn();
			}

			result.HeaderText = dataColumn.Caption;
			result.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
			result.DataPropertyName = dataColumn.ColumnName;
			result.ReadOnly = false;
			result.SortMode = DataGridViewColumnSortMode.Automatic;

			return result;
		}

		[NotNull]
		private static DataGridViewColumn CreateBooleanComboboxColumn()
		{
			var result =
				new DataGridViewComboBoxColumn
				{
					DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox,
					DisplayStyleForCurrentCellOnly = true
				};

			result.Items.Add(true);
			result.Items.Add(false);

			return result;
		}

		[NotNull]
		private static DataGridViewColumn CreateEnumComboboxColumn(
			[NotNull] TestParameter testParameter)
		{
			var result =
				new DataGridViewComboBoxColumn
				{
					DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox,
					DisplayStyleForCurrentCellOnly = true
				};

			Array values = Enum.GetValues(testParameter.Type);
			var items = new List<EnumValueItem>();

			for (var i = 0; i < values.Length; i++)
			{
				items.Add(new EnumValueItem(values.GetValue(i)));
			}

			result.DataSource = items;
			result.DisplayMember = "Name";
			result.ValueMember = "Value";

			return result;
		}

		[NotNull]
		private DataColumn CreateQualityConditionNameColumn(bool fill)
		{
			var column = new DataColumn(_qualityConditionNameColumnName, typeof(string))
			             {
				             Caption = _qualityConditionNameColumnCaption,
				             ReadOnly = false
			             };

			var gridColumn =
				new DataGridViewTextBoxColumn
				{
					HeaderText = column.Caption,
					DataPropertyName = _qualityConditionNameColumnName,
					ReadOnly = false,
					SortMode = DataGridViewColumnSortMode.Automatic,
					MinimumWidth = 150,
					AutoSizeMode = fill
						               ? DataGridViewAutoSizeColumnMode.Fill
						               : DataGridViewAutoSizeColumnMode.AllCells
				};

			_view.AddParametersColumn(gridColumn);

			return column;
		}

		[NotNull]
		private static DataColumn CreateDatasetColumn()
		{
			var column = new DataColumn(_datasetColumnName, typeof(Dataset));

			// no corresponding datagrid column

			return column;
		}

		[NotNull]
		private DataColumn CreateCategoryNameColumn()
		{
			var column = new DataColumn(_categoryColumnName, typeof(string))
			             {
				             Caption = _categoryColumnCaption,
				             ReadOnly = false
			             };

			var gridColumn =
				new DataGridViewTextBoxColumn
				{
					HeaderText = column.Caption,
					AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
					DataPropertyName = _categoryColumnName,
					ReadOnly = true,
					SortMode = DataGridViewColumnSortMode.Automatic
				};

			_view.AddParametersColumn(gridColumn);
			return column;
		}

		[NotNull]
		private DataColumn CreateFilterExpressionColumn()
		{
			var column = new DataColumn(_filterExpressionColumnName, typeof(string))
			             {
				             Caption = _filterExpressionColumnCaption,
				             ReadOnly = false
			             };

			var gridColumn =
				new DataGridViewTextBoxColumn
				{
					HeaderText = column.Caption,
					AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
					MinimumWidth = 110,
					DataPropertyName = _filterExpressionColumnName,
					ReadOnly = false,
					SortMode = DataGridViewColumnSortMode.Automatic,
					Resizable = DataGridViewTriState.True
				};

			_view.AddParametersColumn(gridColumn);
			return column;
		}

		[NotNull]
		private DataColumn CreateModelNameColumn()
		{
			var column = new DataColumn(_modelNameColumnName, typeof(string))
			             {
				             Caption = _modelNameColumnCaption,
				             ReadOnly = false
			             };

			var gridColumn =
				new DataGridViewTextBoxColumn
				{
					HeaderText = column.Caption,
					AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
					DataPropertyName = _modelNameColumnName,
					ReadOnly = true,
					SortMode = DataGridViewColumnSortMode.Automatic
				};

			_view.AddParametersColumn(gridColumn);
			return column;
		}

		[NotNull]
		private DataColumn CreateDatasetNameColumn([NotNull] string datasetParameterName)
		{
			var column = new DataColumn(_datasetNameColumnName, typeof(string))
			             {
				             Caption = datasetParameterName,
				             ReadOnly = false
			             };

			var gridColumn =
				new DataGridViewTextBoxColumn
				{
					HeaderText = column.Caption,
					AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
					DataPropertyName = _datasetNameColumnName,
					ReadOnly = true,
					SortMode = DataGridViewColumnSortMode.Automatic
				};

			_view.AddParametersColumn(gridColumn);

			return column;
		}

		[NotNull]
		private DataColumn CreateImageColumn()
		{
			var column = new DataColumn(_imageColumnName, typeof(Image))
			             {
				             Caption = _imageColumnCaption,
				             ReadOnly = false
			             };

			var gridColumn = new DataGridViewImageColumn
			                 {
				                 HeaderText = string.Empty,
				                 Width = 20,
				                 AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
				                 DataPropertyName = _imageColumnName,
				                 ReadOnly = true
			                 };

			_view.AddParametersColumn(gridColumn);

			return column;
		}

		[NotNull]
		private IEnumerable<Dataset> GetSelectedDatasets()
		{
			return _parametersDataTable.Rows.Cast<DataRow>().Select(GetDataset).ToList();
		}

		private static bool TryCreateName([NotNull] IDdxDataset dataset,
		                                  [CanBeNull] DataQualityCategory
			                                  dataQualityCategory,
		                                  [NotNull] string naming,
		                                  [NotNull] out string name,
		                                  [NotNull] out List<string> errorMessages)
		{
			var sb = new StringBuilder(naming);
			var errors = new List<string>();

			foreach (KeyValuePair<string, string> pair in
			         GetNameValuePairs(dataset, dataQualityCategory))
			{
				string token = pair.Key;
				string value = pair.Value;

				if (naming.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0)
				{
					continue;
				}

				if (StringUtils.IsNullOrEmptyOrBlank(value))
				{
					errors.Add(string.Format("Value for {0} is not defined", token));
				}
				else
				{
					sb.Replace(token, value);
				}
			}

			if (errors.Count == 0)
			{
				name = sb.ToString();
				errorMessages = new List<string>();
				return true;
			}

			name = string.Empty;
			errorMessages = errors;
			return false;
		}

		#region Nested types

		private class EnumValueItem
		{
			private readonly int _value;
			private readonly string _name;

			public EnumValueItem([NotNull] object enumValue)
			{
				_value = (int) enumValue;
				_name = string.Format("{0}", enumValue);
			}

			[UsedImplicitly]
			public int Value => _value;

			[UsedImplicitly]
			public string Name => _name;
		}

		#endregion
	}
}
