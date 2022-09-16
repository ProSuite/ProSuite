using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.QA
{
	public class RowFilterConfiguration : InstanceConfiguration
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RowFilterConfiguration" /> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		[UsedImplicitly]
		public RowFilterConfiguration() : base(assignUuid: false) { }

		public RowFilterConfiguration(string name,
		                              [NotNull] RowFilterDescriptor rowFilterDescriptor,
		                              [CanBeNull] string description = "")
			: base(name, description)
		{
			Assert.ArgumentNotNull(rowFilterDescriptor, nameof(rowFilterDescriptor));

			RowFilterDescriptor = rowFilterDescriptor;
		}

		[Required]
		public RowFilterDescriptor RowFilterDescriptor
		{
			get => (RowFilterDescriptor) InstanceDescriptor;
			private set => InstanceDescriptor = value;
		}

		#region Overrides of InstanceConfiguration

		public override string TypeDisplayName => "Row Filter";

		public override InstanceConfiguration CreateCopy()
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
