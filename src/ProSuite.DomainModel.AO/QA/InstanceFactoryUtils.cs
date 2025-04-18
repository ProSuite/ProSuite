using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.QA
{
	public static class InstanceFactoryUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Gets the instance factory, sets the instance configuration and initializes its 
		/// parameter values.
		/// </summary>
		/// <returns>InstanceFactory or null.</returns>
		[CanBeNull]
		public static InstanceFactory CreateFactory(
			[NotNull] InstanceConfiguration instanceConfiguration)
		{
			Assert.ArgumentNotNull(instanceConfiguration, nameof(instanceConfiguration));

			if (instanceConfiguration.InstanceDescriptor == null)
			{
				return null;
			}

			if (instanceConfiguration is TransformerConfiguration transConfig)
			{
				return CreateTransformerFactory(transConfig);
			}

			if (instanceConfiguration is IssueFilterConfiguration issueFilterConfig)
			{
				return CreateIssueFilterFactory(issueFilterConfig);
			}

			if (instanceConfiguration is QualityCondition qualityCondition)
			{
				return TestFactoryUtils.CreateTestFactory(qualityCondition);
			}

			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the issue filter factory, sets the issue filter configuration and initializes its 
		/// parameter values.
		/// </summary>
		/// <returns>IssueFilterFactory or null.</returns>
		[CanBeNull]
		public static IssueFilterFactory CreateIssueFilterFactory(
			[NotNull] IssueFilterConfiguration issueFilterConfig)
		{
			Assert.ArgumentNotNull(issueFilterConfig, nameof(issueFilterConfig));

			if (issueFilterConfig.InstanceDescriptor == null)
			{
				return null;
			}

			IssueFilterFactory factory =
				CreateIssueFilterFactory(issueFilterConfig.IssueFilterDescriptor);

			if (factory != null)
			{
				InstanceConfigurationUtils.InitializeParameterValues(
					factory, issueFilterConfig);
			}

			return factory;
		}

		/// <summary>
		/// Gets the transformer factory, sets the transformer configuration and initializes its 
		/// parameter values.
		/// </summary>
		/// <returns>TransformerFactory or null.</returns>
		[CanBeNull]
		public static TransformerFactory CreateTransformerFactory(
			[NotNull] TransformerConfiguration transformerConfiguration)
		{
			Assert.ArgumentNotNull(transformerConfiguration, nameof(transformerConfiguration));

			if (transformerConfiguration.TransformerDescriptor == null)
			{
				return null;
			}

			TransformerFactory factory =
				CreateTransformerFactory(transformerConfiguration.TransformerDescriptor);

			if (factory != null)
			{
				InstanceConfigurationUtils.InitializeParameterValues(
					factory, transformerConfiguration);
			}

			return factory;
		}

		[NotNull]
		public static IEnumerable<Type> GetTransformerClasses([NotNull] Assembly assembly,
		                                                      bool includeObsolete,
		                                                      bool includeInternallyUsed)
		{
			Assert.ArgumentNotNull(assembly, nameof(assembly));

			Type transformerType = typeof(ITableTransformer);

			return GetClasses(assembly, transformerType, includeObsolete, includeInternallyUsed);
		}

		[NotNull]
		public static IEnumerable<Type> GetIssueFilterClasses([NotNull] Assembly assembly,
		                                                      bool includeObsolete,
		                                                      bool includeInternallyUsed)
		{
			Assert.ArgumentNotNull(assembly, nameof(assembly));

			Type filterType = typeof(IIssueFilter);

			return GetClasses(assembly, filterType, includeObsolete, includeInternallyUsed);
		}

		[NotNull]
		public static IEnumerable<Type> GetClasses([NotNull] Assembly assembly,
		                                           [NotNull] Type baseType,
		                                           bool includeObsolete,
		                                           bool includeInternallyUsed)
		{
			Assert.ArgumentNotNull(assembly, nameof(assembly));

			foreach (Type candidateType in assembly.GetTypes())
			{
				if (! InstanceUtils.IsInstanceType(candidateType, baseType))
				{
					continue;
				}

				if (! includeObsolete && InstanceUtils.IsObsolete(candidateType))
				{
					continue;
				}

				if (! includeInternallyUsed && InstanceUtils.IsInternallyUsed(candidateType))
				{
					continue;
				}

				yield return candidateType;
			}
		}

		public static StringBuilder GetErrorMessageWithDetails(
			[NotNull] InstanceConfiguration forInstanceConfiguration,
			[NotNull] Exception exception)
		{
			var sb = new StringBuilder();

			string typeName = forInstanceConfiguration.GetType().Name;

			sb.AppendFormat("Unable to create {0} {1}",
			                typeName, forInstanceConfiguration.Name);
			sb.AppendLine();
			sb.AppendLine("with parameters:");

			foreach (TestParameterValue value in forInstanceConfiguration.ParameterValues)
			{
				string stringValue;
				try
				{
					stringValue = value.StringValue;
				}
				catch (Exception e1)
				{
					_msg.Debug(
						$"Error getting string value for parameter {value.TestParameterName} " +
						$"of {typeName} {forInstanceConfiguration.Name}", e1);

					stringValue = $"<error: {e1.Message} (see log for details)>";
				}

				sb.AppendFormat("  {0} : {1}", value.TestParameterName, stringValue);
				sb.AppendLine();
			}

			sb.AppendFormat("error message: {0}",
			                ExceptionUtils.GetInnermostMessage(exception));
			sb.AppendLine();

			return sb;
		}

		[CanBeNull]
		private static IssueFilterFactory CreateIssueFilterFactory(
			[NotNull] IssueFilterDescriptor descriptor)
		{
			Assert.ArgumentNotNull(descriptor, nameof(descriptor));

			ClassDescriptor classDescriptor = descriptor.Class;

			return classDescriptor != null
				       ? new IssueFilterFactory(classDescriptor.AssemblyName,
				                                classDescriptor.TypeName,
				                                descriptor.ConstructorId)
				       : null;
		}

		[CanBeNull]
		private static TransformerFactory CreateTransformerFactory(
			[NotNull] TransformerDescriptor descriptor)
		{
			Assert.ArgumentNotNull(descriptor, nameof(descriptor));

			ClassDescriptor classDescriptor = descriptor.Class;

			return classDescriptor != null
				       ? new TransformerFactory(classDescriptor.AssemblyName,
				                                classDescriptor.TypeName,
				                                descriptor.ConstructorId)
				       : null;
		}

		public static string GetDefaultDescriptorName([NotNull] Type instanceType,
		                                              int constructorIndex)
		{
			Assert.ArgumentNotNull(instanceType, nameof(instanceType));

			return $"{GetDescriptorBaseName(instanceType)}({constructorIndex})";
		}

		[NotNull]
		private static string GetDescriptorBaseName([NotNull] Type type)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			string result = type.Name.Trim();

			if (result.Length > 2 &&
			    result.StartsWith("tr", StringComparison.InvariantCultureIgnoreCase))
			{
				result = result.Substring(2);
			}
			else if (result.Length > 2 &&
			         result.StartsWith("if", StringComparison.InvariantCultureIgnoreCase))
			{
				result = result.Substring(2);
			}
			
			return result;
		}
	}
}
