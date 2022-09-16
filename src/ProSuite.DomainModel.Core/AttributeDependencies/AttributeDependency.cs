using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;
using ProSuite.DomainModel.Core.DataModel;
using Attribute = ProSuite.DomainModel.Core.DataModel.Attribute;

namespace ProSuite.DomainModel.Core.AttributeDependencies
{
	/// <summary>
	/// Domain data model class for AttributeDependency
	/// </summary>
	public class AttributeDependency : VersionedEntityWithMetadata
	{
		[UsedImplicitly] private ObjectDataset _dataset;

		// TODO Consider lists of attribute names, ie, strings
		[UsedImplicitly] private readonly IList<Attribute> _sourceAttributes =
			new List<Attribute>();

		[UsedImplicitly] private readonly IList<Attribute> _targetAttributes =
			new List<Attribute>();

		[UsedImplicitly] private readonly IList<AttributeValueMapping>
			_attributeValueMappings = new List<AttributeValueMapping>();

		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		public AttributeDependency() { }

		public AttributeDependency([NotNull] ObjectDataset dataset) : this()
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			_dataset = dataset;
		}

		#endregion

		[Required]
		public ObjectDataset Dataset
		{
			get { return _dataset; }
			[UsedImplicitly] set { _dataset = value; }
		}

		[NotNull]
		public IList<Attribute> SourceAttributes
		{
			get { return _sourceAttributes; }
		}

		[NotNull]
		public IList<Attribute> TargetAttributes
		{
			get { return _targetAttributes; }
		}

		[NotNull]
		public IList<AttributeValueMapping> AttributeValueMappings
		{
			get { return _attributeValueMappings; }
		}

		/// <summary>
		/// Can we go from target values to source values?
		/// </summary>
		public bool CanReverse
		{
			get { return true; }
			// Once we've source queries (instead of discrete values), this will be false
		}

		public override string ToString()
		{
			return string.Format("AttributeDependency for {0}",
			                     _dataset?.ToString() ?? "<dataset not assigned>");
		}
	}
}
