using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.QA
{
	public class IssueFilterConfiguration : InstanceConfiguration
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IssueFilterConfiguration" /> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		[UsedImplicitly]
		public IssueFilterConfiguration() : this(assignUuid: false) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="IssueFilterConfiguration" /> class.
		/// </summary>
		[UsedImplicitly]
		public IssueFilterConfiguration(bool assignUuid) : base(assignUuid) { }

		public IssueFilterConfiguration(string name,
		                                [NotNull] IssueFilterDescriptor issueFilterDescriptor,
		                                [CanBeNull] string description = "",
		                                bool assignUuid = true)
			: base(name, description, assignUuid)
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

		public override string TypeDisplayName => "Issue Filter";

		[NotNull]
		public override InstanceConfiguration CreateCopy()
		{
			var copy = new IssueFilterConfiguration(assignUuid: true);

			CopyProperties(copy);

			return copy;
		}

		#endregion

		public override string ToString()
		{
			return $"Issue Filter Configuration '{Name}'";
		}

		private void CopyProperties(IssueFilterConfiguration target)
		{
			Assert.ArgumentNotNull(target, nameof(target));

			CopyBaseProperties(target);

			target.IssueFilterDescriptor = IssueFilterDescriptor;
		}
	}
}
