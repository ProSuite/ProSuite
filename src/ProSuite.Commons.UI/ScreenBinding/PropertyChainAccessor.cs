using System;
using System.Collections.Generic;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Validation;

namespace ProSuite.Commons.UI.ScreenBinding
{
	public class PropertyChainAccessor : IPropertyAccessor
	{
		private readonly PropertyInfo[] _chain;
		private readonly SinglePropertyAccessor _innerPropertyAccessor;

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyChainAccessor"/> class.
		/// </summary>
		/// <param name="properties">The properties.</param>
		public PropertyChainAccessor(PropertyInfo[] properties)
		{
			Assert.ArgumentNotNull(properties, nameof(properties));

			_chain = new PropertyInfo[properties.Length - 1];
			for (var i = 0; i < _chain.Length; i++)
			{
				_chain[i] = properties[i];
			}

			_innerPropertyAccessor =
				new SinglePropertyAccessor(properties[properties.Length - 1]);
		}

		#region IPropertyAccessor Members

		public void SetValue(object target, object propertyValue)
		{
			target = FindInnerMostTarget(target);
			if (target == null)
			{
				return;
			}

			_innerPropertyAccessor.SetValue(target, propertyValue);
		}

		public object GetValue(object target)
		{
			target = FindInnerMostTarget(target);

			if (target == null)
			{
				return null;
			}

			return _innerPropertyAccessor.GetValue(target);
		}

		public string FieldName => _innerPropertyAccessor.FieldName;

		public Type PropertyType => _innerPropertyAccessor.PropertyType;

		public PropertyInfo InnerProperty => _innerPropertyAccessor.InnerProperty;

		//public IAccessor GetChildAccessor<T>(Expression<Func<T, object>> expression)
		//{
		//    PropertyInfo property = ReflectionHelper.GetProperty(expression);

		//    List<PropertyInfo> list = new List<PropertyInfo>(_chain);
		//    list.Add(_innerPropertyAccessor.InnerProperty);
		//    list.Add(property);

		//    return new PropertyChainAccessor(list.ToArray());
		//}

		// TODO revise
		public IPropertyAccessor GetChildAccessor<T>(string propertyName)
		{
			PropertyInfo property = typeof(T).GetProperty(propertyName);

			// PropertyInfo property = ReflectionHelper.GetProperty(expression);

			var list = new List<PropertyInfo>(_chain);
			list.Add(_innerPropertyAccessor.InnerProperty);
			list.Add(property);

			return new PropertyChainAccessor(list.ToArray());
		}

		public NotificationMessage[] Validate(object target)
		{
			object innerTarget = FindInnerMostTarget(target);
			return _innerPropertyAccessor.Validate(innerTarget);
		}

		public string Name
		{
			get
			{
				string returnValue = string.Empty;
				foreach (PropertyInfo info in _chain)
				{
					returnValue += info.Name;
				}

				returnValue += _innerPropertyAccessor.Name;

				return returnValue;
			}
		}

		#endregion

		private object FindInnerMostTarget(object target)
		{
			foreach (PropertyInfo propertyInfo in _chain)
			{
				target = propertyInfo.GetValue(target, null);
				if (target == null)
				{
					return null;
				}
			}

			return target;
		}
	}
}
