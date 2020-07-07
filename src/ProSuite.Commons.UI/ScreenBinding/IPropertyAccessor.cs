using System;
using System.Reflection;
using ProSuite.Commons.Validation;

namespace ProSuite.Commons.UI.ScreenBinding
{
	public interface IPropertyAccessor
	{
		string FieldName { get; }

		Type PropertyType { get; }

		PropertyInfo InnerProperty { get; }

		void SetValue(object target, object propertyValue);

		object GetValue(object target);

		IPropertyAccessor GetChildAccessor<T>(string propertyName);

		// IPropertyAccessor GetChildAccessor<T>(Expression<Func<T, object>> expression);

		NotificationMessage[] Validate(object target);

		string Name { get; }
	}
}
