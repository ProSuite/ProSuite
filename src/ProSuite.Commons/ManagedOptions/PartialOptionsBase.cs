using System;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.ManagedOptions
{
	public abstract class PartialOptionsBase
	{
		[NotNull]
		public abstract PartialOptionsBase Clone();

		/// <summary>
		/// Gets the overridable setting of the specified property.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="propertyName">The name of the property in this class.</param>
		/// <returns></returns>
		[CanBeNull]
		public OverridableSetting<T> GetOverridableSetting<T>([NotNull] string propertyName)
			where T : struct
		{
			Assert.ArgumentNotNullOrEmpty(propertyName, nameof(propertyName));

			var result = (OverridableSetting<T>) GetPropertyValue(this, propertyName);

			return result;
		}

		/// <summary>
		/// Gets the overridable setting of the specified property.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="propertyInfo"></param>
		/// <returns></returns>
		[CanBeNull]
		public OverridableSetting<T> GetOverridableSetting<T>(
			[NotNull] PropertyInfo propertyInfo) where T : struct
		{
			Assert.ArgumentNotNull(propertyInfo, nameof(propertyInfo));

			var result =
				(OverridableSetting<T>) GetPropertyValue(this, propertyInfo);

			return result;
		}

		/// <summary>
		/// Gets the overridable setting of the specified property.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="propertyName">The name of the property in this class</param>
		/// <param name="initialValueToUseIfNull">If the value of the property is null, the property is initialized
		/// with this value.</param>
		/// <returns></returns>
		[NotNull]
		public OverridableSetting<T> GetOverridableSetting<T>([NotNull] string propertyName,
		                                                      T? initialValueToUseIfNull)
			where T : struct
		{
			Assert.ArgumentNotNullOrEmpty(propertyName, nameof(propertyName));

			PropertyInfo propertyInfo = GetPropertyInfo(this, propertyName);

			return GetOverridableSetting(propertyInfo, initialValueToUseIfNull);
		}

		/// <summary>
		/// Gets the overridable setting of the specified property.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="propertyInfo"></param>
		/// <param name="initialValueToUseIfNull">If the value of the property is null, the property is initialized
		/// with this value.</param>
		/// <returns></returns>
		[NotNull]
		public OverridableSetting<T> GetOverridableSetting<T>(
			[NotNull] PropertyInfo propertyInfo,
			T? initialValueToUseIfNull) where T : struct
		{
			Assert.ArgumentNotNull(propertyInfo, nameof(propertyInfo));

			var result = (OverridableSetting<T>) GetPropertyValue(this, propertyInfo);

			if (result == null)
			{
				SetOverridableSetting(propertyInfo,
				                      new OverridableSetting<T>(initialValueToUseIfNull, false));
			}

			result = (OverridableSetting<T>) GetPropertyValue(this, propertyInfo);

			return Assert.NotNull(result, "overridable setting is null");
		}

		public void SetOverridableSetting<T>([NotNull] PropertyInfo propertyInfo,
		                                     [NotNull] OverridableSetting<T> value)
			where T : struct
		{
			propertyInfo.SetValue(this, value, null);
		}

		[CanBeNull]
		private static object GetPropertyValue([NotNull] object fromObj,
		                                       [NotNull] PropertyInfo propertyInfo)
		{
			return propertyInfo.GetValue(fromObj, null);
		}

		[CanBeNull]
		private static object GetPropertyValue([NotNull] object fromObj,
		                                       [NotNull] string propertyName)
		{
			PropertyInfo propertyInfo = GetPropertyInfo(fromObj, propertyName);

			return propertyInfo.GetValue(fromObj, null);
		}

		[NotNull]
		private static PropertyInfo GetPropertyInfo([NotNull] object fromObj,
		                                            [NotNull] string propertyName)
		{
			Type type = fromObj.GetType();

			PropertyInfo propertyInfo = type.GetProperty(propertyName);

			Assert.NotNull(propertyInfo, "Type {0} has no property with name {1}", type,
			               propertyName);

			return propertyInfo;
		}

		private static void SetPropertyValue([NotNull] object toObject,
		                                     [NotNull] string propertyName,
		                                     [CanBeNull] object value)
		{
			PropertyInfo info = GetPropertyInfo(toObject, propertyName);

			info.SetValue(toObject, value, null);
		}

		[CanBeNull]
		protected static OverridableSetting<T> TryClone<T>(
			[CanBeNull] OverridableSetting<T> setting) where T : struct
		{
			return setting?.Clone();
		}
	}
}