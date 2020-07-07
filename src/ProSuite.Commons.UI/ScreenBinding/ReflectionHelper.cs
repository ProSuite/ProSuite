using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Reflection;

namespace ProSuite.Commons.UI.ScreenBinding
{
	public static class ReflectionHelper
	{
		[NotNull]
		public static PropertyInfo GetProperty<MODEL>(
			[NotNull] Expression<Func<MODEL, object>> expression)
		{
			MemberExpression memberExpression = ReflectionUtils.GetMemberExpression(expression);
			return (PropertyInfo) memberExpression.Member;
		}

		[NotNull]
		public static IPropertyAccessor GetPropertyAccessor<T>([NotNull] string propertyName)
		{
			return new SinglePropertyAccessor(ReflectionUtils.GetProperty<T>(propertyName));
		}

		[NotNull]
		public static IPropertyAccessor GetPropertyAccessor<MODEL>(
			[NotNull] Expression<Func<MODEL, object>> expression)
		{
			MemberExpression memberExpression = ReflectionUtils.GetMemberExpression(expression);

			return GetAccessor(memberExpression);
		}

		[NotNull]
		public static IPropertyAccessor GetPropertyAccessor<MODEL, T>(
			[NotNull] Expression<Func<MODEL, T>> expression)
		{
			MemberExpression memberExpression = ReflectionUtils.GetMemberExpression(expression);

			return GetAccessor(memberExpression);
		}

		[NotNull]
		private static IPropertyAccessor GetAccessor(
			[NotNull] MemberExpression memberExpression)
		{
			MemberExpression expression = memberExpression;

			var list = new List<PropertyInfo>();

			while (expression != null)
			{
				list.Add((PropertyInfo) expression.Member);
				expression = expression.Expression as MemberExpression;
			}

			if (list.Count == 1)
			{
				return new SinglePropertyAccessor(list[0]);
			}

			list.Reverse();
			return new PropertyChainAccessor(list.ToArray());
		}
	}
}
