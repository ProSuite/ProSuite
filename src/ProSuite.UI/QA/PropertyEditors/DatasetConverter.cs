using System;
using System.ComponentModel;
using ProSuite.DomainModel.AO.QA;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class DatasetConverter : TypeConverter
	{
		public override bool GetPropertiesSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		public override PropertyDescriptorCollection GetProperties(
			ITypeDescriptorContext context, object value, Attribute[] attributes)
		{
			var valueModel = (IQualityConditionContextAware) value;
			var contextState = (IQualityConditionContextAware) context.Instance;

			valueModel.DatasetProvider = contextState.DatasetProvider;
			valueModel.QualityCondition = contextState.QualityCondition;

			return TypeDescriptor.GetProperties(value.GetType(), attributes);
		}
	}
}
