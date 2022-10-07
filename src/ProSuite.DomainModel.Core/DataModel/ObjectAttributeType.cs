using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class ObjectAttributeType : AttributeType
	{
		[UsedImplicitly] private bool _readOnly;
		[UsedImplicitly] private AttributeRole _attributeRole;
		[UsedImplicitly] private bool _isObjectDefining;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectAttributeType"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		[UsedImplicitly]
		protected ObjectAttributeType() { }

		public ObjectAttributeType(string name) : base(name) { }

		public ObjectAttributeType(AttributeRole role)
			: base(GetDefaultTypeName(role))
		{
			_attributeRole = role;
		}

		public ObjectAttributeType([NotNull] string name, AttributeRole role)
			: base(name)
		{
			_attributeRole = role;
		}

		#endregion

		public bool ReadOnly
		{
			get { return _readOnly; }
			set { _readOnly = value; }
		}

		public AttributeRole AttributeRole
		{
			get { return _attributeRole; }
			set { _attributeRole = value; }
		}

		public bool IsObjectDefining
		{
			get { return _isObjectDefining; }
			set { _isObjectDefining = value; }
		}

		[NotNull]
		private static string GetDefaultTypeName([NotNull] AttributeRole role)
		{
			Assert.ArgumentNotNull(role, nameof(role));

			return AttributeRole.GetName(role);
		}
	}
}
