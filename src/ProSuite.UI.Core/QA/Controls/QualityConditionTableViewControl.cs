using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.UI.Core.QA.Controls
{
	public partial class QualityConditionTableViewControl : UserControl,
	                                                        IInstanceConfigurationTableViewControl
	{
		[CanBeNull] private TestDescriptor _testDescriptor;

		public QualityConditionTableViewControl()
		{
			InitializeComponent();

			_dataGridViewTestParameters.AutoGenerateColumns = false;
			_columnParameterValueFilterExpression.CellTemplate = new DatasetPropertyCell();
			_columnParameterValueModelName.CellTemplate = new DatasetPropertyCell();
			_columnParameterUsedAsReferenceData.CellTemplate = new DatasetPropertyCell();
		}

		private bool ShowDescription
		{
			get { return ! _splitContainer.Panel2Collapsed; }
			set { _splitContainer.Panel2Collapsed = ! value; }
		}

		public void SetQualityCondition([CanBeNull] QualityCondition qualityCondition)
		{
			_testDescriptor = null;
			var paramValues = new BindingList<ParameterValueListItem>();

			if (qualityCondition != null)
			{
				_testDescriptor = qualityCondition.TestDescriptor;
				var paramDictionary = new Dictionary<string, List<TestParameterValue>>();
				foreach (TestParameterValue paramValue in qualityCondition.ParameterValues)
				{
					List<TestParameterValue> valueList;
					if (! paramDictionary.TryGetValue(paramValue.TestParameterName, out valueList))
					{
						valueList = new List<TestParameterValue>();
						paramDictionary.Add(paramValue.TestParameterName, valueList);
					}

					valueList.Add(paramValue);
				}

				foreach (List<TestParameterValue> valueList in paramDictionary.Values)
				{
					foreach (TestParameterValue paramValue in valueList)
					{
						paramValues.Add(new ParameterValueListItem(paramValue));
					}
				}
			}

			BindToParameterValues(paramValues);
			ShowDescription = true;
		}

		public void BindToParameterValues(
			[NotNull] BindingList<ParameterValueListItem> parameterValueItems)
		{
			_bindingSourceParametrValueList.DataSource = parameterValueItems;
			ShowDescription = false;
		}

		public void BindTo(InstanceConfiguration qualityCondition) { }

		[CanBeNull]
		private string GetParameterDescription([CanBeNull] string parameterName)
		{
			if (parameterName == null || _testDescriptor == null)
			{
				return null;
			}

			var testFactory = InstanceDescriptorUtils.GetInstanceInfo(_testDescriptor);
			return testFactory?.Parameters
			                  .FirstOrDefault(x => x.Name == parameterName)
			                  ?.Description;
		}

		private void _dataGridViewTestParameters_CurrentCellChanged(object sender,
			EventArgs e)
		{
			if (! ShowDescription)
			{
				return;
			}

			var parameterItem =
				_dataGridViewTestParameters.CurrentRow?.DataBoundItem as ParameterValueListItem;

			_parameterDescriptionTextBox.Text =
				GetParameterDescription(parameterItem?.ParameterName);
		}

		private void _dataGridViewTestParameters_DataBindingComplete(object sender,
			DataGridViewBindingCompleteEventArgs
				e)
		{
			_dataGridViewTestParameters.ClearSelection();
		}

		#region nested classes

		private class DatasetPropertyCell : DataGridViewTextBoxCell
		{
			public override Type EditType
			{
				get
				{
					ParameterValueListItem item = GetItem(RowIndex);

					return item.IsDataset
						       ? base.EditType
						       : null;
				}
			}

			[NotNull]
			private static object CloneFields([NotNull] object original,
			                                  [NotNull] object baseClonedObject)
			{
				Type t = baseClonedObject.GetType();
				foreach (FieldInfo fieldInfo in t.GetFields(BindingFlags.Public |
				                                            BindingFlags.NonPublic |
				                                            BindingFlags.Instance |
				                                            BindingFlags.SetField |
				                                            BindingFlags.GetField |
				                                            BindingFlags.DeclaredOnly))
				{
					fieldInfo.SetValue(baseClonedObject, fieldInfo.GetValue(original));
				}

				return baseClonedObject;
			}

			// Example call in an ICloneable object
			public override object Clone()
			{
				object clone = CloneFields(this, base.Clone());
				return clone;
			}

			protected override void Paint(Graphics graphics, Rectangle clipBounds,
			                              Rectangle cellBounds,
			                              int rowIndex,
			                              DataGridViewElementStates cellState,
			                              object value,
			                              object formattedValue, string errorText,
			                              DataGridViewCellStyle cellStyle,
			                              DataGridViewAdvancedBorderStyle advancedBorderStyle,
			                              DataGridViewPaintParts paintParts)
			{
				ParameterValueListItem item = GetItem(rowIndex);
				if (item.IsDataset)
				{
					base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState,
					           value, formattedValue, errorText, cellStyle,
					           advancedBorderStyle, paintParts);
				}
				else
				{
					graphics.FillRectangle(Brushes.LightGray, cellBounds);
				}
			}

			[NotNull]
			private ParameterValueListItem GetItem(int rowIndex)
			{
				Assert.NotNull(DataGridView, "DataGridView");

				return (ParameterValueListItem) Assert.NotNull(
					DataGridView.Rows[rowIndex].DataBoundItem);
			}
		}

		#endregion
	}
}
