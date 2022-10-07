using System;
using System.ComponentModel;
using System.Globalization;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class ConstraintConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return sourceType == typeof(string);
		}

		public override object ConvertFrom(ITypeDescriptorContext context,
		                                   CultureInfo culture, object value)
		{
			return value is string
				       ? value
				       : base.ConvertFrom(context, culture, value);
		}

		public override bool IsValid(ITypeDescriptorContext context, object value)
		{
			return true;
		}
	}
}
