using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.QA
{
	public class IssueFilterConfiguration : InstanceConfiguration
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RowFilterConfiguration" /> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		[UsedImplicitly]
		public IssueFilterConfiguration() { }

		public IssueFilterConfiguration(string name,
		                                [NotNull] IssueFilterDescriptor issueFilterDescriptor,
		                                [CanBeNull] string description = "")
			: base(name, description)
		{
			Assert.ArgumentNotNull(issueFilterDescriptor, nameof(issueFilterDescriptor));

			IssueFilterDescriptor = issueFilterDescriptor;
		}

		[Required]
		public IssueFilterDescriptor IssueFilterDescriptor
		{
			get => (IssueFilterDescriptor) InstanceDescriptor;
			private set => InstanceDescriptor = value;
		}

		#region Overrides of InstanceConfiguration

		public override DataQualityCategory Category { get; set; }

		#endregion
	}
}
