using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Reflection;
using ProSuite.QA.Core.IssueCodes;

namespace ProSuite.QA.Core.Reports
{
	public class IncludedTestFactory : IncludedInstance, IComparable<IncludedTestFactory>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly Type _testFactoryType;

		public IncludedTestFactory([NotNull] Type testFactoryType)
			: base(GetTitle(testFactoryType),
			       testFactoryType.Assembly,
			       GetInstanceInfo(testFactoryType),
			       InstanceUtils.IsObsolete(testFactoryType),
			       InstanceUtils.IsInternallyUsed(testFactoryType))
		{
			_testFactoryType = testFactoryType;
		}

		private static string GetTitle(Type testFactoryType)
		{
			return testFactoryType.Name;
		}

		private static IInstanceInfo GetInstanceInfo(Type testFactoryType)
		{
			Type factoryDefinitionType = GetTestFactoryDefinitionType(testFactoryType);

			if (factoryDefinitionType != null)
			{
				return (TestFactoryDefinition) Activator.CreateInstance(factoryDefinitionType);
			}

			ConstructorInfo ctor = testFactoryType.GetConstructors()[0];

			return (IInstanceInfo) ctor.Invoke(new object[] { });
		}

		[CanBeNull]
		private static Type GetTestFactoryDefinitionType(Type testFactoryType)
		{
			Assembly testFactoryAssembly = testFactoryType.Assembly;

			string fullPath = Path.Combine(
				ReflectionUtils.GetAssemblyDirectory(testFactoryAssembly),
				$"{InstanceUtils.GetDefinitionsAssemblyName(testFactoryAssembly.GetName().Name)}.dll");

			Assembly definitionsAssembly;
			try
			{
				definitionsAssembly = Assembly.LoadFrom(fullPath);
			}
			catch (Exception e)
			{
				_msg.Warn($"Assembly could not be loaded: {fullPath}", e);
				return null;
			}

			string definitionName =
				InstanceUtils.GetAlgorithmDefinitionName(testFactoryType.FullName);
			Type factoryDefType = definitionsAssembly.GetType(definitionName);

			return factoryDefType;
		}

		#region Overrides of IncludedInstanceBase

		public override string Key => Assert.NotNull(_testFactoryType.FullName, "FullName");

		public override Type InstanceType => _testFactoryType;

		public override IList<IssueCode> IssueCodes =>
			IssueCodeUtils.GetIssueCodes(_testFactoryType);

		#endregion

		#region IComparable<IncludedTestFactory> Members

		public int CompareTo(IncludedTestFactory other)
		{
			return base.CompareTo(other);
		}

		#endregion
	}
}
