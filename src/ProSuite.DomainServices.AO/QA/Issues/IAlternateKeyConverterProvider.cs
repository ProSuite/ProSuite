using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public interface IAlternateKeyConverterProvider
	{
		[CanBeNull]
		IAlternateKeyConverter GetConverter(Guid qualityConditionGuid);
	}
}
