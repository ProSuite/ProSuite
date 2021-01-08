using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	[CLSCompliant(false)]
	public interface IAttributeConfiguratorFactory
	{
		IAttributeConfigurator Create();

		IAttributeConfigurator Create(
			[CanBeNull] IEnumerable<AttributeType> existingAttributeTypes);
	}
}
