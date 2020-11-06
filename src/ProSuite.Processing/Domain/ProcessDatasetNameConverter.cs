using System;
using System.ComponentModel;
using System.Globalization;

namespace ProSuite.Processing.Domain
{
	public class ProcessDatasetNameConverter : TypeConverter
	{
		public override object ConvertFrom(ITypeDescriptorContext context,
		                                   CultureInfo culture, object value)
		{
			if (value is ProcessDatasetName datasetName)
				return datasetName;

			var text = Convert.ToString(value);
			if (! string.IsNullOrEmpty(text))
				return ProcessDatasetName.Parse(text);

			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context,
		                                 CultureInfo culture, object value,
		                                 Type destinationType)
		{
			if (destinationType == typeof(string))
			{
				return value.ToString();
			}

			return base.ConvertTo(context, culture, value, destinationType);
		}
	}
}
