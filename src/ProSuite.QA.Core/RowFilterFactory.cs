using System;
using System.Collections.Generic;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Reflection;

namespace ProSuite.QA.Core
{
	/// <summary>
	/// Factory for IRowFilter instances.
	/// </summary>
	public class RowFilterFactory : ParameterizedInstanceFactory
	{
		[UsedImplicitly] [NotNull] private readonly Type _filterType;
		[UsedImplicitly] private readonly int _constructorId;

		/// <summary>
		/// Initializes a new instance of the <see cref="RowFilterFactory"/> class.
		/// </summary>
		public RowFilterFactory([NotNull] Type type, int constructorId = 0)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			_filterType = type;
			_constructorId = constructorId;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RowFilterFactory"/> class.
		/// </summary>
		public RowFilterFactory([NotNull] string assemblyName,
		                        [NotNull] string typeName,
		                        int constructorId = 0)
		{
			Assert.ArgumentNotNull(assemblyName, nameof(assemblyName));
			Assert.ArgumentNotNull(typeName, nameof(typeName));

			_filterType = PrivateAssemblyUtils.LoadType(assemblyName, typeName);

			if (_filterType == null)
			{
				throw new TypeLoadException(
					string.Format("{0} does not exist in {1}", typeName, assemblyName));
			}

			if (_filterType.GetConstructors().Length <= constructorId)
			{
				throw new TypeLoadException(
					string.Format("invalid constructorId {0}, {1} has {2} constructors",
					              constructorId, typeName, _filterType.GetConstructors().Length));
			}

			_constructorId = constructorId;
		}

		public Type FilterType => _filterType;

		#region ParameterizedInstanceFactory overrides

		[NotNull]
		public override string[] TestCategories => ReflectionUtils.GetCategories(GetType());

		public override string GetTestDescription()
		{
			ConstructorInfo ctor = FilterType.GetConstructors()[_constructorId];

			return ParameterizedInstanceUtils.GetDescription(ctor);
		}

		public override string GetTestTypeDescription()
		{
			return FilterType.Name;
		}

		protected override IList<TestParameter> CreateParameters()
		{
			return ParameterizedInstanceUtils.CreateParameters(FilterType, _constructorId);
		}

		#endregion
	}
}
