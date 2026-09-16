using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.QA.TestReport;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.AlgorithmUI
{
	/// <summary>
	/// The single place that decides which documentation page a descriptor gets: the
	/// algorithm page when a provider is registered and can produce one, the classic
	/// per-constructor report otherwise.
	/// </summary>
	/// <remarks>
	/// Two things show that page - the "Documentation" command and the help pane the
	/// condition and configuration forms refresh whenever they load an entity - and they
	/// have to agree. Otherwise opening the algorithm page and then selecting another
	/// condition replaces it with the classic report.
	/// </remarks>
	public static class InstanceDocumentationPage
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static string Render(
			[CanBeNull] IInstanceDocumentationProvider documentationProvider,
			[NotNull] InstanceDescriptor descriptor,
			[CanBeNull] out string title)
		{
			Assert.ArgumentNotNull(descriptor, nameof(descriptor));

			if (documentationProvider != null &&
			    documentationProvider.TryGetDocumentation(
				    descriptor, out string algorithmTitle, out string algorithmHtml))
			{
				title = algorithmTitle;
				return algorithmHtml;
			}

			// Says why the classic report appears where the algorithm page is expected:
			// either no provider is registered here, or it cannot document this
			// descriptor's algorithm.
			_msg.Debug(
				$"No algorithm documentation page for {descriptor.Name} " +
				$"(provider registered: {documentationProvider != null}); " +
				"showing the classic per-constructor report.");

			title = descriptor.TypeDisplayName;
			return TestReportUtils.WriteDescriptorDoc(descriptor);
		}
	}
}
