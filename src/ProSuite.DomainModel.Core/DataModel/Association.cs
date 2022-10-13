using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public abstract class Association : ModelElement
	{
		[UsedImplicitly] private AssociationEnd _end1;
		[UsedImplicitly] private AssociationEnd _end2;
		[UsedImplicitly] private bool _notUsedForDerivedTableGeometry;

		/// <summary>
		/// Cardinality of the association
		/// </summary>
		[UsedImplicitly] private AssociationCardinality _cardinality =
			AssociationCardinality.Unknown;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="Association"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected Association() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Association"/> class.
		/// </summary>
		/// <param name="name">The name of the relationship class.</param>
		/// <param name="cardinality">The cardinality.</param>
		protected Association([NotNull] string name, AssociationCardinality cardinality)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));
			AssertValidCardinality(cardinality);

			Name = name;
			_cardinality = cardinality;
		}

		private void AssertValidCardinality(AssociationCardinality cardinality)
		{
			Assert.ArgumentCondition(IsValidCardinality(cardinality),
			                         "Unsupported cardinality");
		}

		#endregion

		[NotNull]
		public AssociationEnd End1
		{
			get { return _end1; }
			protected set
			{
				Assert.ArgumentNotNull(value, nameof(value));
				_end1 = value;
			}
		}

		[NotNull]
		public AssociationEnd End2
		{
			get { return _end2; }
			protected set
			{
				Assert.ArgumentNotNull(value, nameof(value));
				_end2 = value;
			}
		}

		[NotNull]
		public AssociationEnd DestinationEnd
		{
			get { return End1; }
			protected set { End1 = value; }
		}

		[NotNull]
		public AssociationEnd OriginEnd
		{
			get { return End2; }
			protected set { End2 = value; }
		}

		[NotNull]
		public ObjectDataset DestinationDataset => DestinationEnd.ObjectDataset;

		[NotNull]
		public ObjectDataset OriginDataset => OriginEnd.ObjectDataset;

		/// <summary>
		/// Gets the cardinality of the association.
		/// </summary>
		/// <value>The cardinality.</value>
		public AssociationCardinality Cardinality
		{
			get { return _cardinality; }
			set
			{
				AssertValidCardinality(value);
				_cardinality = value;
			}
		}

		public abstract bool IsAttributed { get; }

		public bool NotUsedForDerivedTableGeometry
		{
			get { return _notUsedForDerivedTableGeometry; }
			set { _notUsedForDerivedTableGeometry = value; }
		}

		public void CopyPropertiesTo([NotNull] Association other)
		{
			Assert.ArgumentNotNull(other, nameof(other));

			other.NotUsedForDerivedTableGeometry = NotUsedForDerivedTableGeometry;

			_end1.CopyPropertiesTo(other.End1);
			_end2.CopyPropertiesTo(other.End2);
		}

		protected abstract bool IsValidCardinality(AssociationCardinality cardinality);
	}
}
