using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.QA
{
	public class RowFilterConfiguration : InstanceConfiguration
	{
		[UsedImplicitly] private RowFilterDescriptor _rowFilterDescriptor;

		/// <summary>
		/// Initializes a new instance of the <see cref="RowFilterConfiguration" /> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected RowFilterConfiguration() { }

		public RowFilterConfiguration(string name,
		                              [NotNull] RowFilterDescriptor rowFilterDescriptor,
		                              [CanBeNull] string description = "")
			: base(name, description)
		{
			Assert.ArgumentNotNull(rowFilterDescriptor, nameof(rowFilterDescriptor));

			_rowFilterDescriptor = rowFilterDescriptor;
		}

		public override InstanceDescriptor InstanceDescriptor => RowFilterDescriptor;

		[Required]
		public RowFilterDescriptor RowFilterDescriptor
		{
			get => _rowFilterDescriptor;
			set => _rowFilterDescriptor = value;
		}
	}
}
