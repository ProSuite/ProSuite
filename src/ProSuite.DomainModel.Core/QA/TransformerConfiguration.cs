using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.QA
{
	public class TransformerConfiguration : InstanceConfiguration
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TransformerConfiguration" /> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		[UsedImplicitly]
		public TransformerConfiguration() : this(assignUuid: false) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="TransformerConfiguration" /> class.
		/// </summary>
		[UsedImplicitly]
		public TransformerConfiguration(bool assignUuid) : base(assignUuid) { }

		public TransformerConfiguration(string name,
		                                [NotNull] TransformerDescriptor transformerDescriptor,
		                                [CanBeNull] string description = "",
		                                bool assignUuid = true)
			: base(name, description, assignUuid)
		{
			Assert.ArgumentNotNull(transformerDescriptor, nameof(transformerDescriptor));

			TransformerDescriptor = transformerDescriptor;
		}

		[Required]
		public TransformerDescriptor TransformerDescriptor
		{
			get => (TransformerDescriptor) InstanceDescriptor;
			private set => InstanceDescriptor = value;
		}

		private object _value;
		private object _datasetContext;

		public bool HasCachedValue(object datasetContext)
		{
			return ReferenceEquals(datasetContext, _datasetContext) && _value != null;
		}

		public object GetCachedValue() => _value;

		public void CacheValue(object value, object datasetContext)
		{
			_value = value;
			_datasetContext = datasetContext;
		}

		#region Overrides of InstanceConfiguration

		public override string TypeDisplayName => "Transformer";

		[NotNull]
		public override InstanceConfiguration CreateCopy()
		{
			var copy = new TransformerConfiguration(assignUuid: true);

			CopyProperties(copy);

			return copy;
		}

		#endregion

		public override string ToString()
		{
			return $"Transformer Configuration '{Name}'";
		}

		private void CopyProperties([NotNull] TransformerConfiguration target)
		{
			Assert.ArgumentNotNull(target, nameof(target));

			CopyBaseProperties(target);

			target.TransformerDescriptor = TransformerDescriptor;
		}
	}
}
