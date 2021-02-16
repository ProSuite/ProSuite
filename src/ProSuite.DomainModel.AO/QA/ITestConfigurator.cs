using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.AO.QA
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
