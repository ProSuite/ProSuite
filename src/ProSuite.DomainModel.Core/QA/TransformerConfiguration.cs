using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.QA
{
	public class TransformerConfiguration : InstanceConfiguration
	{
		[UsedImplicitly] private TransformerDescriptor _transformerDescriptor;

		/// <summary>
		/// Initializes a new instance of the <see cref="TransformerConfiguration" /> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected TransformerConfiguration() { }

		public TransformerConfiguration(string name,
		                                [NotNull] TransformerDescriptor transformerDescriptor,
		                                [CanBeNull] string description = "")
			: base(name, description)
		{
			Assert.ArgumentNotNull(transformerDescriptor, nameof(transformerDescriptor));

			_transformerDescriptor = transformerDescriptor;
		}

		public override InstanceDescriptor InstanceDescriptor => TransformerDescriptor;

		[Required]
		public TransformerDescriptor TransformerDescriptor
		{
			get => _transformerDescriptor;
			set => _transformerDescriptor = value;
		}
	}
}
