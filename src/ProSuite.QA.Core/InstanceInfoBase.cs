using System;
using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Core
{
	public abstract class InstanceInfoBase : ITestImplementationInfo
	{
		private IList<TestParameter> _parameters;

		[NotNull]
		public IList<TestParameter> Parameters
		{
			get
			{
				if (_parameters == null)
				{
					_parameters = CreateParameters();
				}

				return new ReadOnlyList<TestParameter>(_parameters);
			}
		}

		public abstract string[] TestCategories { get; }

		[NotNull]
		public TestParameter GetParameter(string parameterName)
		{
			Assert.ArgumentNotNullOrEmpty(parameterName, nameof(parameterName));

			foreach (TestParameter parameter in Parameters)
			{
				if (string.Equals(parameterName, parameter.Name,
				                  StringComparison.OrdinalIgnoreCase))
				{
					return parameter;
				}
			}

			throw new ArgumentException(string.Format("Unknown parameter: {0} {1}",
			                                          parameterName, GetTestTypeDescription()));
		}

		public abstract string GetTestDescription();

		[CanBeNull]
		public virtual string GetParameterDescription([NotNull] string parameterName)
		{
			// TODO: revise, case-insensitive match is ok? (parameter name search is insensitive elsewhere)
			foreach (TestParameter parameter in Parameters)
			{
				if (string.Equals(parameter.Name, parameterName,
				                  StringComparison.OrdinalIgnoreCase))
				{
					return parameter.Description;
				}
			}

			return null;
		}

		public abstract string GetTestTypeDescription();

		protected abstract IList<TestParameter> CreateParameters();
	}
}