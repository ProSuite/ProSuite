using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.QA
{
	/// <summary>
	/// The assignment of an <see cref="AttributeRole"/> to a field of the dataset referenced by a
	/// dataset test parameter value.
	/// <para>
	/// A DDX dataset carries its attribute roles itself, so this type is only needed where there
	/// is no DDX: in the standalone paths the model is harvested from the live workspace, where a
	/// raster catalog is indistinguishable from any other polygon feature class. Transporting the
	/// role assignments alongside the parameter value lets the standalone factory build the same
	/// dataset a DDX would have provided.
	/// </para>
	/// </summary>
	public class DatasetFieldRole : IEquatable<DatasetFieldRole>
	{
		public DatasetFieldRole([NotNull] AttributeRole role, [NotNull] string fieldName)
		{
			Role = role ?? throw new ArgumentNullException(nameof(role));

			if (string.IsNullOrWhiteSpace(fieldName))
			{
				throw new ArgumentNullException(nameof(fieldName));
			}

			FieldName = fieldName;
		}

		[NotNull]
		public AttributeRole Role { get; }

		/// <summary>
		/// The name of the field in the geodatabase that plays <see cref="Role"/>.
		/// </summary>
		[NotNull]
		public string FieldName { get; }

		public bool Equals(DatasetFieldRole other)
		{
			if (other == null)
			{
				return false;
			}

			return Role.Equals(other.Role) &&
			       string.Equals(FieldName, other.FieldName, StringComparison.OrdinalIgnoreCase);
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as DatasetFieldRole);
		}

		public override int GetHashCode()
		{
			return Role.GetHashCode() ^
			       StringComparer.OrdinalIgnoreCase.GetHashCode(FieldName);
		}

		public override string ToString()
		{
			return $"{AttributeRole.GetSimpleName(Role)}={FieldName}";
		}
	}
}
