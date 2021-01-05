using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	[CLSCompliant(false)]
	public interface IWorkspaceContextLookup
	{
		[CanBeNull]
		IWorkspaceContext GetWorkspaceContext([NotNull] IDdxDataset dataset);
	}
}
