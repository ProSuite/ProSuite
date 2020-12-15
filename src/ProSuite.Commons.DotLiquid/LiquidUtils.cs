using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DotLiquid;
using DotLiquid.FileSystems;
using DotLiquid.NamingConventions;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.DotLiquid
{
	public static class LiquidUtils
	{
		static LiquidUtils()
		{
			Template.NamingConvention = new CSharpNamingConvention();
			Template.RegisterFilter(typeof(CustomFilters));
		}

		[PublicAPI]
		public static void RegisterSafeType<T>(bool recurse = true)
		{
			RegisterSafeType(typeof(T), recurse);
		}

		[PublicAPI]
		public static void RegisterSafeType([NotNull] Type type, bool recurse)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			RegisterSafeType(type, new HashSet<Type>(), recurse);
		}

		[NotNull]
		[PublicAPI]
		public static string Render([NotNull] string templatePath,
		                            [NotNull] object model,
		                            [CanBeNull] string modelName = null,
		                            bool rethrowErrors = false)
		{
			Assert.ArgumentNotNullOrEmpty(templatePath, nameof(templatePath));
			Assert.ArgumentNotNull(model, nameof(model));
			Assert.ArgumentCondition(File.Exists(templatePath),
			                         "template file does not exist: {0}", templatePath);

			Hash hash = GetHash(model, modelName);

			return Render(templatePath, hash, rethrowErrors);
		}

		[NotNull]
		[PublicAPI]
		public static string Render([NotNull] string templatePath,
		                            params KeyValuePair<string, object>[] modelsByName)
		{
			return Render(templatePath, false, modelsByName);
		}

		[NotNull]
		[PublicAPI]
		public static string Render([NotNull] string templatePath,
		                            bool rethrowErrors,
		                            params KeyValuePair<string, object>[] modelsByName)
		{
			Assert.ArgumentNotNullOrEmpty(templatePath, nameof(templatePath));
			Assert.ArgumentCondition(File.Exists(templatePath),
			                         "template file does not exist: {0}", templatePath);
			Assert.ArgumentCondition(modelsByName.Length > 0,
			                         "at least one name/model pair must be specified");

			Dictionary<string, object> dictionary = modelsByName.ToDictionary(
				pair => pair.Key, pair => pair.Value);

			Hash hash = Hash.FromDictionary(dictionary);

			return Render(templatePath, hash, rethrowErrors);
		}

		#region Non-public

		[NotNull]
		private static string Render([NotNull] string templatePath,
		                             [NotNull] Hash hash,
		                             bool rethrowErrors = false)
		{
			string templateDirectory = Path.GetDirectoryName(Path.GetFullPath(templatePath));

			IFileSystem origFileSystem = Template.FileSystem;

			try
			{
				if (! string.IsNullOrEmpty(templateDirectory))
				{
					Template.FileSystem = new LocalFileSystem(templateDirectory);
				}

				string templateString = FileSystemUtils.ReadTextFile(templatePath);

				Template template = Template.Parse(templateString);
				var parameters = new RenderParameters
				                 {
					                 LocalVariables = hash,
					                 RethrowErrors = rethrowErrors
				                 };

				string result = template.Render(parameters) ?? string.Empty;

				return result;
			}
			finally
			{
				Template.FileSystem = origFileSystem;
			}
		}

		[NotNull]
		private static Hash GetHash([NotNull] object model, [CanBeNull] string modelName)
		{
			if (StringUtils.IsNullOrEmptyOrBlank(modelName))
			{
				return Hash.FromAnonymousObject(model);
			}

			return Hash.FromDictionary(new Dictionary<string, object>
			                           {
				                           {modelName, Hash.FromAnonymousObject(model)}
			                           });
		}

		private static void RegisterSafeType([NotNull] Type type,
		                                     [NotNull] ICollection<Type> registeredTypes,
		                                     bool recurse)
		{
			if (registeredTypes.Contains(type))
			{
				return;
			}

			PropertyInfo[] properties = type.GetProperties();

			string[] propertyNames = properties.Select(m => m.Name).ToArray();

			registeredTypes.Add(type);
			Template.RegisterSafeType(type, propertyNames);

			if (! recurse)
			{
				return;
			}

			foreach (PropertyInfo property in properties)
			{
				Type propertyType = property.PropertyType;

				RegisterSafeType(propertyType, registeredTypes, true);

				if (propertyType.IsGenericType)
				{
					foreach (Type typeParameter in propertyType.GetGenericArguments())
					{
						RegisterSafeType(typeParameter, registeredTypes, true);
					}
				}
			}
		}

		//private static void LogBindableProperties(
		//    [NotNull] IEnumerable<KeyValuePair<string, object>> bindableProperties)
		//{
		//    _msg.VerboseDebug("Bindable properties:");

		//    using (_msg.IncrementIndentation())
		//    {
		//        foreach (KeyValuePair<string, object> pair in bindableProperties)
		//        {
		//            LogBindableProperty(pair.Key, pair.Value);
		//        }
		//    }
		//}

		//private static void LogBindableProperty([NotNull] string name,
		//                                        [CanBeNull] object value)
		//{
		//    var hash = value as IDictionary<string, object>;
		//    if (hash == null)
		//    {
		//        _msg.VerboseDebugFormat("{0}: {1}", name, value ?? "<null>");
		//    }
		//    else
		//    {
		//        _msg.VerboseDebugFormat("{0} ({1} properties)", name, hash.Count);

		//        using (_msg.IncrementIndentation())
		//        {
		//            foreach (KeyValuePair<string, object> pair in hash)
		//            {
		//                LogBindableProperty(pair.Key, pair.Value);
		//            }
		//        }
		//    }
		//}

		#endregion
	}
}