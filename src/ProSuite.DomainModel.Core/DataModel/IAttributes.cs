using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public interface IAttributes
	{
		[NotNull]
		IList<Attribute> Attributes { get; }

		[CanBeNull]
		Attribute GetAttribute([NotNull] string name, bool includeDeleted = false);
	}
}
