using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.QA
{
	/// <summary>
	/// Narrow-focus, simple to implement interface for opening any DDX datasets (and associations)
	/// that are supported on a specific platform for a specific context.
	/// </summary>
	public interface IOpenDataset
	{
		[CanBeNull]
		object OpenDataset([NotNull] IDdxDataset dataset,
		                   [CanBeNull] Type knownType = null);

		bool IsSupportedType([NotNull] Type dataType);
	}
}
