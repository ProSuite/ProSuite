using System;
using System.ComponentModel;

namespace ProSuite.Commons.AO.Geodatabase.SchemaInfo
{
	internal class AllPropertiesConverter : TypeConverter
	{
		public override bool GetPropertiesSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		public override PropertyDescriptorCollection
			GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
		{
			return TypeDescriptor.GetProperties(value.GetType());
		}
	}
}
