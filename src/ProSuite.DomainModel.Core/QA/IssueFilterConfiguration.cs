using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.QA
{
	public class IssueFilterConfiguration : InstanceConfiguration
	{
		[UsedImplicitly] private IssueFilterDescriptor _issueFilterDescriptor;

		/// <summary>
		/// Initializes a new instance of the <see cref="RowFilterConfiguration" /> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected IssueFilterConfiguration() { }

		public IssueFilterConfiguration(string name,
		                                [NotNull] IssueFilterDescriptor issueFilterDescriptor,
		                                [CanBeNull] string description = "")
			: base(name, description)
		{
			Assert.ArgumentNotNull(issueFilterDescriptor, nameof(issueFilterDescriptor));

			_issueFilterDescriptor = issueFilterDescriptor;
		}

		public override InstanceDescriptor InstanceDescriptor => IssueFilterDescriptor;

		[Required]
		public IssueFilterDescriptor IssueFilterDescriptor
		{
			get => _issueFilterDescriptor;
			set => _issueFilterDescriptor = value;
		}
	}
}
