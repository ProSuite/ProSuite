using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA
{
	public interface ITestConfigurator : IQualityConditionContextAware
	{
		event EventHandler DataChanged;

		string GetTestDescription();

		[NotNull]
		IList<TestParameterValue> GetTestParameterValues();

		[CanBeNull]
		Type GetTestClassType();

		int GetTestConstructorId();

		[CanBeNull]
		Type GetFactoryType();

		void SyncParameterValues();
	}
}
