using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Reflection
{
	public static class ReflectionUtils
	{
		/// <summary>
		/// Determines whether the specified property is browsable 
		/// (i.e. it is readable and does not have the browsable attribute set to false).
		/// </summary>
		/// <param name="propertyInfo">The property info.</param>
		/// <returns>
		///   <c>true</c> if the specified property is browsable; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsBrowsable([NotNull] PropertyInfo propertyInfo)
		{
			Assert.ArgumentNotNull(propertyInfo, nameof(propertyInfo));

			if (! propertyInfo.CanRead)
			{
				return false;
			}

			object[] attributes = propertyInfo.GetCustomAttributes(
				typeof(BrowsableAttribute), inherit: true);

			return attributes.OfType<BrowsableAttribute>()
			                 .All(browsableAttribute => browsableAttribute.Browsable);
		}

		/// <summary>
		/// Determines whether the specified attribute provider has an Obsolete attribute.
		/// </summary>
		/// <param name="attributeProvider">The attribute provider.</param>
		/// <returns>
		/// 	<c>true</c> if the specified attribute provider is obsolete; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsObsolete(
			[NotNull] ICustomAttributeProvider attributeProvider)
		{
			return IsObsolete(attributeProvider, out string _);
		}

		/// <summary>
		/// Determines whether the specified attribute provider has an Obsolete attribute.
		/// </summary>
		/// <param name="attributeProvider">The attribute provider.</param>
		/// <param name="message"></param>
		/// <returns>
		/// 	<c>true</c> if the specified attribute provider is obsolete; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsObsolete(
			[NotNull] ICustomAttributeProvider attributeProvider,
			[CanBeNull] out string message)
		{
			Assert.ArgumentNotNull(attributeProvider, nameof(attributeProvider));

			object[] obsoleteAttributes = attributeProvider.GetCustomAttributes(
				typeof(ObsoleteAttribute), inherit: true);

			bool isObsolete = obsoleteAttributes.Length > 0;
			if (! isObsolete)
			{
				message = null;
				return false;
			}

			// get the message from the first 'obsolete' attribute (there should only be one)
			var obsoleteAttribute = obsoleteAttributes[0] as ObsoleteAttribute;
			message = obsoleteAttribute?.Message;

			return true;
		}

		/// <summary>
		/// Determines whether the specified attribute provider has an attribute of a given type.
		/// </summary>
		/// <typeparam name="T">The attribute type</typeparam>
		/// <param name="attributeProvider">The attribute provider.</param>
		/// <param name="inherit">When true, look up the hierarchy chain for the inherited custom attribute.</param>
		/// <param name="match">An optional predicate for the attribute instance(s) on the type.</param>
		/// <returns>
		///   <c>true</c> if the specified attribute provider has an attribute of the specified type; otherwise, <c>false</c>.
		/// </returns>
		public static bool HasAttribute<T>(
			[NotNull] ICustomAttributeProvider attributeProvider,
			bool inherit = false,
			[CanBeNull] Predicate<T> match = null) where T : Attribute
		{
			Assert.ArgumentNotNull(attributeProvider, nameof(attributeProvider));

			return GetAttribute(attributeProvider, inherit, match) != null;
		}

		/// <summary>
		/// Gets the first attribute of a given type and optionally matching a predicate.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="attributeProvider">The attribute provider.</param>
		/// <param name="inherit">When true, look up the hierarchy chain for the inherited custom attribute.</param>
		/// <param name="match">The match.</param>
		/// <returns></returns>
		[CanBeNull]
		public static T GetAttribute<T>(
			[NotNull] ICustomAttributeProvider attributeProvider,
			bool inherit = false,
			[CanBeNull] Predicate<T> match = null) where T : Attribute
		{
			Assert.ArgumentNotNull(attributeProvider, nameof(attributeProvider));

			object[] attributes = attributeProvider.GetCustomAttributes(typeof(T),
				inherit);

			if (attributes.Length == 0)
			{
				return null; // none found
			}

			if (match == null)
			{
				// at least one found, no further conditions
				return (T) attributes[0];
			}

			// evaluate the predicate
			return attributes.OfType<T>().FirstOrDefault(typed => match(typed));
		}

		[CanBeNull]
		public static string GetDescription([NotNull] ICustomAttributeProvider element,
		                                    bool inherit = true)
		{
			Assert.ArgumentNotNull(element, nameof(element));

			object[] attributes = element.GetCustomAttributes(typeof(DescriptionAttribute),
			                                                  inherit);

			// DescriptionAttribute has AttributeUsage AllowMultiple=false,
			// so the first DescriptionAttribute we find will be the only one.
			return attributes.OfType<DescriptionAttribute>()
			                 .Select(desc => desc.Description)
			                 .FirstOrDefault();
		}

		[NotNull]
		public static string[] GetCategories([NotNull] ICustomAttributeProvider element,
		                                     bool inherit = true)
		{
			Assert.ArgumentNotNull(element, nameof(element));

			object[] attributes = element.GetCustomAttributes(typeof(CategoryAttribute),
			                                                  inherit);

			return attributes.OfType<CategoryAttribute>()
			                 .Select(categoryAttribute => categoryAttribute.Category)
			                 .ToArray();
		}

		public static bool TryGetDefaultValue(
			[NotNull] ICustomAttributeProvider attributeProvider,
			[CanBeNull] out object defaultValue)
		{
			var defaultValueAttribute =
				GetAttribute<DefaultValueAttribute>(attributeProvider);

			if (defaultValueAttribute != null)
			{
				defaultValue = defaultValueAttribute.Value;
				return true;
			}

			defaultValue = null;
			return false;
		}

		/// <remarks>
		/// Use default(T) if you know the type T at compile-time!
		/// </remarks>
		public static object GetDefaultValue(this Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			// For all value types V, their default value is new V(), and
			// this default constructor always exists (created by compiler);
			// the exception is Nullable<T>, which is a value type, but has
			// as default value null!
			if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
			{
				// hopefully this is fast for value types!
				return Activator.CreateInstance(type);
			}

			return null;
		}

		/// <summary>
		/// Get the value of the named "constant" (static field).
		/// Non-public fields and fields from base classes are included.
		/// </summary>
		public static object GetConstantValue(this Type type, string constantName)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));
			if (constantName is null)
				return null;

			const BindingFlags flags = BindingFlags.Static | // constants are "static"
			                           BindingFlags.FlattenHierarchy | // include base classes
			                           BindingFlags.Public | BindingFlags.NonPublic;

			var field = type.GetField(constantName, flags);
			return field?.GetValue(null);
		}

		[NotNull]
		public static string GetAssemblyVersionString([NotNull] Assembly assembly)
		{
			Assert.ArgumentNotNull(assembly, nameof(assembly));

			return assembly.GetName().Version.ToString();
		}

		[NotNull]
		public static string GetAssemblyFileVersionString([NotNull] Assembly assembly)
		{
			Assert.ArgumentNotNull(assembly, nameof(assembly));

			string location = assembly.Location;

			return string.IsNullOrEmpty(location)
				       ? "<n/a>"
				       : FileVersionInfo.GetVersionInfo(location).FileVersion;
		}

		[NotNull]
		public static string GetAssemblyDirectory([NotNull] Assembly assembly)
		{
			var assemblyFile = new FileInfo(assembly.Location);

			var binDirectory = Assert.NotNull(assemblyFile.Directory);

			return binDirectory.FullName;
		}

		/// <summary>
		/// Gets the property info for a given property on the specified type
		/// </summary>
		/// <typeparam name="T">The type to get the property info from.</typeparam>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns></returns>
		[NotNull]
		public static PropertyInfo GetProperty<T>([NotNull] string propertyName)
		{
			Assert.ArgumentNotNullOrEmpty(propertyName, nameof(propertyName));

			return GetProperty(typeof(T), propertyName);
		}

		/// <summary>
		/// Gets the property info for a given property on the specified type
		/// </summary>
		/// <param name="type">The type to get the property info from.</param>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns></returns>
		[NotNull]
		public static PropertyInfo GetProperty([NotNull] Type type,
		                                       [NotNull] string propertyName)
		{
			PropertyInfo propertyInfo = type.IsInterface
				                            ? GetFromInterface(type, propertyName)
				                            : type.GetProperty(propertyName);

			if (propertyInfo == null)
			{
				throw new ArgumentException(
					string.Format("Property '{0}' does not exist on type {1}",
					              propertyName, type.Name),
					nameof(propertyName));
			}

			return propertyInfo;
		}

		[NotNull]
		public static PropertyInfo[] GetPublicProperties([NotNull] Type type)
		{
			if (! type.IsInterface)
			{
				return type.GetProperties(BindingFlags.FlattenHierarchy
				                          | BindingFlags.Public | BindingFlags.Instance);
			}

			// ReSharper disable once CollectionNeverUpdated.Local
			var propertyInfos = new List<PropertyInfo>();

			var considered = new List<Type>();
			var queue = new Queue<Type>();
			considered.Add(type);
			queue.Enqueue(type);
			while (queue.Count > 0)
			{
				Type subType = queue.Dequeue();
				foreach (Type subInterface in subType.GetInterfaces())
				{
					if (considered.Contains(subInterface))
					{
						continue;
					}

					considered.Add(subInterface);
					queue.Enqueue(subInterface);
				}

				PropertyInfo[] typeProperties = subType.GetProperties(
					BindingFlags.FlattenHierarchy
					| BindingFlags.Public
					| BindingFlags.Instance);

				IEnumerable<PropertyInfo> newPropertyInfos = typeProperties
					.Where(x => ! propertyInfos.Contains(x));

				propertyInfos.InsertRange(0, newPropertyInfos);
			}

			return propertyInfos.ToArray();
		}

		[CanBeNull]
		private static PropertyInfo GetFromInterface([NotNull] Type interfaceType,
		                                             [NotNull] string propertyName)
		{
			var considered = new List<Type>();
			var queue = new Queue<Type>();

			considered.Add(interfaceType);
			queue.Enqueue(interfaceType);

			while (queue.Count > 0)
			{
				Type subType = queue.Dequeue();
				foreach (Type subInterface in subType.GetInterfaces())
				{
					if (considered.Contains(subInterface))
					{
						continue;
					}

					considered.Add(subInterface);
					queue.Enqueue(subInterface);
				}

				PropertyInfo propertyInfo = subType.GetProperty(
					propertyName,
					BindingFlags.FlattenHierarchy
					| BindingFlags.Public
					| BindingFlags.Instance);

				if (propertyInfo != null)
				{
					return propertyInfo;
				}
			}

			return null;
		}

		[CanBeNull]
		public static object GetPropertyValue([NotNull] object obj,
		                                      [NotNull] string propertyName)
		{
			Assert.ArgumentNotNull(obj, nameof(obj));
			Assert.ArgumentNotNullOrEmpty(propertyName, nameof(propertyName));

			Type type = obj.GetType();

			PropertyInfo propertyInfo = GetProperty(type, propertyName);

			return propertyInfo.GetValue(obj, null);
		}

		/// <summary>
		/// Gets the PropertyInfo of the property called in the specified expression.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="propertyExpression"></param>
		/// <returns></returns>
		[NotNull]
		public static PropertyInfo GetProperty<T>(
			[NotNull] Expression<Func<T>> propertyExpression)
		{
			Assert.ArgumentNotNull(propertyExpression, nameof(propertyExpression));

			var memberExpression = propertyExpression.Body as MemberExpression;

			Assert.NotNull(memberExpression, "memberExpression is null");

			return Assert.NotNull(memberExpression.Member as PropertyInfo,
			                      "Property does not exist on the type");
		}

		[NotNull]
		public static PropertyInfo GetProperty<MODEL, T>(
			[NotNull] Expression<Func<MODEL, T>> expression)
		{
			Assert.ArgumentNotNull(expression, nameof(expression));

			MemberExpression memberExpression = GetMemberExpression(expression);
			return (PropertyInfo) memberExpression.Member;
		}

		/// <summary>
		/// Gets the name of the property Getter method for a given property name.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns></returns>
		[NotNull]
		public static string GetPropertyGetMethodName([NotNull] string propertyName)
		{
			Assert.ArgumentNotNullOrEmpty(propertyName, nameof(propertyName));

			return "Get__" + propertyName;
		}

		/// <summary>
		/// Gets the name of the property Setter method for a given property name.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns></returns>
		[NotNull]
		public static string GetPropertySetMethodName([NotNull] string propertyName)
		{
			Assert.ArgumentNotNullOrEmpty(propertyName, nameof(propertyName));

			return "Set__" + propertyName;
		}

		/// <summary>
		/// Gets the full name of a type, including generic type arguments, in the format
		/// that allows its use in code.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		[CanBeNull]
		public static string GetFullName([CanBeNull] Type type)
		{
			if (type == null)
			{
				return null;
			}

			if (type.IsGenericType)
			{
				Type genericType = type.GetGenericTypeDefinition();
				Assert.NotNull(genericType, "generic type is null");

				string genericTypeName = genericType.FullName;
				Assert.NotNull(genericTypeName, "generic type full name is null");

				int last = genericTypeName.LastIndexOf('`');

				var sb = new StringBuilder(genericTypeName.Substring(0, last));

				IList<Type> args = type.GetGenericArguments();

				if (args.Count > 0)
				{
					var first = true;
					foreach (Type arg in args)
					{
						string argumentTypeName = GetFullName(arg);

						if (string.IsNullOrEmpty(argumentTypeName))
						{
							continue;
						}

						sb.Append(first
							          ? "<"
							          : ",");

						sb.Append(GetFullName(arg));

						first = false;
					}

					if (! first)
					{
						sb.Append(">");
					}
				}

				return sb.ToString();
			}

			// not a generic type
			string full = type.FullName;

			if (! string.IsNullOrEmpty(full))
			{
				full = full.Replace("+", ".");
			}

			return full;
		}

		public static IEnumerable<Type> GetTypes(Assembly fromAssembly,
		                                         Predicate<Type> predicate = null)
		{
			foreach (Type candidateType in fromAssembly.GetTypes())
			{
				if (predicate == null || predicate(candidateType))
				{
					yield return candidateType;
				}
			}
		}

		public static int GetPublicTypeCount([NotNull] Assembly assembly,
		                                     out int classCount,
		                                     out int interfaceCount,
		                                     out int structCount)
		{
			Assert.ArgumentNotNull(assembly, nameof(assembly));

			Type[] publicTypes = assembly.GetExportedTypes();

			classCount = 0;
			interfaceCount = 0;
			structCount = 0;

			foreach (Type type in publicTypes)
			{
				if (type.IsClass)
				{
					classCount++;
				}
				else if (type.IsInterface)
				{
					interfaceCount++;
				}
				else if (type.IsValueType)
				{
					structCount++;
				}
			}

			return publicTypes.Length;
		}

		public static int GetPublicMemberCount([NotNull] Assembly assembly,
		                                       out int methodCount,
		                                       out int propertyCount,
		                                       out int fieldCount,
		                                       out int nestedTypeCount,
		                                       out int eventCount,
		                                       out int constructorCount)
		{
			Assert.ArgumentNotNull(assembly, nameof(assembly));

			Type[] publicTypes = assembly.GetExportedTypes();

			var totalCount = 0;
			methodCount = 0;
			propertyCount = 0;
			fieldCount = 0;
			nestedTypeCount = 0;
			eventCount = 0;
			constructorCount = 0;

			const BindingFlags bindingFlags = BindingFlags.Instance |
			                                  BindingFlags.Static |
			                                  BindingFlags.DeclaredOnly |
			                                  BindingFlags.Public;

			foreach (Type type in publicTypes)
			{
				totalCount += type.GetMembers(bindingFlags).Length;

				methodCount += type.GetMethods(bindingFlags).Length;
				propertyCount += type.GetProperties(bindingFlags).Length;
				fieldCount += type.GetFields(bindingFlags).Length;
				nestedTypeCount += type.GetNestedTypes(bindingFlags).Length;
				eventCount += type.GetEvents(bindingFlags).Length;
				constructorCount += type.GetConstructors(bindingFlags).Length;
			}

			return totalCount;
		}

		[NotNull]
		public static MemberExpression GetMemberExpression<MODEL, T>(
			[NotNull] Expression<Func<MODEL, T>> expression)
		{
			Assert.ArgumentNotNull(expression, nameof(expression));

			MemberExpression memberExpression = null;

			if (expression.Body.NodeType == ExpressionType.Convert)
			{
				var body = (UnaryExpression) expression.Body;
				memberExpression = body.Operand as MemberExpression;
			}
			else if (expression.Body.NodeType == ExpressionType.MemberAccess)
			{
				memberExpression = expression.Body as MemberExpression;
			}

			if (memberExpression == null)
			{
				throw new ArgumentException(@"Not a member access", nameof(expression));
			}

			return memberExpression;
		}

		[NotNull]
		public static MethodInfo GetMethod<T>(
			[NotNull] Expression<Func<T, object>> expression)
		{
			Assert.ArgumentNotNull(expression, nameof(expression));

			var methodCall = (MethodCallExpression) expression.Body;
			return methodCall.Method;
		}

		[NotNull]
		public static MethodInfo GetMethod<T, U>(
			[NotNull] Expression<Func<T, U>> expression)
		{
			Assert.ArgumentNotNull(expression, nameof(expression));

			var methodCall = (MethodCallExpression) expression.Body;
			return methodCall.Method;
		}

		[NotNull]
		public static MethodInfo GetMethod<T, U, V>(
			[NotNull] Expression<Func<T, U, V>> expression)
		{
			Assert.ArgumentNotNull(expression, nameof(expression));

			var methodCall = (MethodCallExpression) expression.Body;
			return methodCall.Method;
		}
	}
}
