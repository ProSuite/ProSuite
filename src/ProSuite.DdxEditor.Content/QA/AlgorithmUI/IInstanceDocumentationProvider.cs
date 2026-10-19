using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.AlgorithmUI
{
	/// <summary>
	/// Seam for a documentation page that knows about algorithms and their parameter
	/// sets, implemented by the algorithm-first UI project
	/// (<c>ProSuite.Shared.DdxEditor.Content</c>) and registered on
	/// <see cref="CoreDomainModelItemModelBuilder.InstanceDocumentation"/>.
	/// </summary>
	/// <remarks>
	/// With no provider registered - the case in every repository that does not wire up
	/// the algorithm-first UI - <c>ShowInstanceWebHelpCommand</c> keeps producing the
	/// classic per-constructor report from <c>TestReportUtils.WriteDescriptorDoc</c>,
	/// unchanged.
	/// </remarks>
	public interface IInstanceDocumentationProvider
	{
		/// <summary>
		/// Produces the documentation page for the algorithm the given descriptor binds
		/// to. Returns <c>false</c> when this provider cannot document it (e.g. the
		/// algorithm's assemblies are not loadable here), in which case the caller falls
		/// back to the classic report rather than showing an error.
		/// </summary>
		bool TryGetDocumentation([NotNull] InstanceDescriptor descriptor,
		                         [CanBeNull] out string title,
		                         [CanBeNull] out string html);
	}
}
