using System;
using System.Linq.Expressions;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Reflection;
using ProSuite.Commons.Validation;

namespace ProSuite.Commons.UI.ScreenBinding
{
	public class SinglePropertyAccessor : IPropertyAccessor
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Initializes a new instance of the <see cref="SinglePropertyAccessor"/> class.
		/// </summary>
		/// <param name="property">The property.</param>
		public SinglePropertyAccessor([NotNull] PropertyInfo property)
		{
			Assert.ArgumentNotNull(property, nameof(property));

			InnerProperty = property;
		}

		#region Accessor Members

		public string FieldName => InnerProperty.Name;

		public Type PropertyType => InnerProperty.PropertyType;

		public PropertyInfo InnerProperty { get; }

		public IPropertyAccessor GetChildAccessor<T>(string propertyName)
		{
			PropertyInfo property = typeof(T).GetProperty(propertyName);

			return new PropertyChainAccessor(new[] {InnerProperty, property});
		}

		[NotNull]
		public IPropertyAccessor GetChildAccessor<T>(
			[NotNull] Expression<Func<T, object>> expression)
		{
			PropertyInfo property = ReflectionHelper.GetProperty(expression);
			return new PropertyChainAccessor(new[] {InnerProperty, property});
		}

		public NotificationMessage[] Validate(object target)
		{
			return target == null
				       ? new NotificationMessage[0]
				       : Validator.ValidateField(target, InnerProperty.Name);
		}

		public string Name => InnerProperty.Name;

		public void SetValue(object target, object propertyValue)
		{
			if (InnerProperty.CanWrite)
			{
				InnerProperty.SetValue(target, propertyValue, null);
			}
			else
			{
				_msg.DebugFormat("Unable to write value to property {0}", InnerProperty.Name);
			}
		}

		public object GetValue(object target)
		{
			Assert.ArgumentNotNull(target, nameof(target));

			return InnerProperty.GetValue(target, null);
		}

		#endregion

		[NotNull]
		public static SinglePropertyAccessor Build<T>(
			[NotNull] Expression<Func<T, object>> expression)
		{
			PropertyInfo property = ReflectionUtils.GetProperty(expression);

			return new SinglePropertyAccessor(property);
		}

		[NotNull]
		public static SinglePropertyAccessor Build<T>([NotNull] string propertyName)
		{
			PropertyInfo property = typeof(T).GetProperty(propertyName);

			return new SinglePropertyAccessor(property);
		}
	}
}
