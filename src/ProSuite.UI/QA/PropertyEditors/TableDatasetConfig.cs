using System.ComponentModel;
using System.Drawing.Design;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class TableDatasetConfig : DatasetConfig
	{
		private string _filterExpression;

		protected override TestParameterType TestParameterTypes
		{
			get { return TestParameterType.TableDataset; }
		}

		public override void SetTestParameterValue(TestParameterValue parameterValue)
		{
			var dsValue = (DatasetTestParameterValue) parameterValue;

			_filterExpression = dsValue?.FilterExpression;

			base.SetTestParameterValue(parameterValue);
		}

		[Editor(typeof(DatasetEditor), typeof(UITypeEditor))]
		[DisplayName("Table")]
		[UsedImplicitly]
		public override Dataset Data
		{
			get { return base.Data; }
			set { base.Data = value; }
		}

		[TypeConverter(typeof(ConstraintConverter))]
		[Description("Use only objects corresponding to filter expression")]
		[DisplayName("Filter Expression")]
		[RefreshProperties(RefreshProperties.All)]
		[UsedImplicitly]
		public string FilterExpression
		{
			get { return _filterExpression; }
			set
			{
				_filterExpression = value;

				var dsValue = (DatasetTestParameterValue) GetTestParameterValue();

				if (dsValue != null)
				{
					dsValue.FilterExpression = _filterExpression;
				}

				OnDataChanged(null);
			}
		}

		public override string ToString()
		{
			if (Data != null)
			{
				// TODO omit ; if FilterExpression is not defined?
				return string.Format("{0};{1}", Data.Name, FilterExpression);
			}

			var value = GetTestParameterValue() as DatasetTestParameterValue;

			if (value?.DatasetValue == null)
			{
				return "(invalid)";
			}

			return string.Empty;
		}
	}
}