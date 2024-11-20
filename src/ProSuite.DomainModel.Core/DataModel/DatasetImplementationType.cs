using System.Collections.Generic;
using System.Reflection;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.DomainModel.Core.DataModel
{
	/// <summary>
	/// Represents an implementation of a dataset type. This is a kind of power-type similar to
	/// AttributeRoles. Also see https://caminao.blog/how-to-implement-symbolic-representations/patterns/functional-patterns/powertypes/.
	/// These primarily help with the transfer of dataset types between different systems.
	/// </summary>
	public class DatasetImplementationType
	{
		private static readonly Dictionary<int, DatasetImplementationType> _types =
			new Dictionary<int, DatasetImplementationType>();

		private static readonly Dictionary<int, string> _typeNames =
			new Dictionary<int, string>();

		#region Static members

		// ReSharper disable MemberCanBePrivate.Global

		// Standard dataset implementation types
		public static readonly DatasetImplementationType Table =
			new DatasetImplementationType((int) DatasetType.Table);

		public static readonly DatasetImplementationType Vector =
			new DatasetImplementationType((int) DatasetType.FeatureClass);

		public static readonly DatasetImplementationType Topology =
			new DatasetImplementationType((int) DatasetType.Topology);

		public static readonly DatasetImplementationType Raster =
			new DatasetImplementationType((int) DatasetType.Raster);

		public static readonly DatasetImplementationType RasterMosaic =
			new DatasetImplementationType((int) DatasetType.RasterMosaic);

		public static readonly DatasetImplementationType Terrain =
			new DatasetImplementationType((int) DatasetType.Terrain);

		// Domain-specific dataset implementation types
		public static readonly DatasetImplementationType ErrorTable =
			new DatasetImplementationType(41);

		public static readonly DatasetImplementationType ErrorMultipoint =
			new DatasetImplementationType(42);

		public static readonly DatasetImplementationType ErrorLine =
			new DatasetImplementationType(43);

		public static readonly DatasetImplementationType ErrorPolygon =
			new DatasetImplementationType(44);

		public static readonly DatasetImplementationType ErrorMultiPatch =
			new DatasetImplementationType(45);

		public static readonly DatasetImplementationType ModelSimpleTerrain =
			new DatasetImplementationType(46);
		// ReSharper restore MemberCanBePrivate.Global

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="DatasetImplementationType"/> class.
		/// </summary>
		/// <param name="id">A unique ID of the DatasetImplementationType.
		/// Note, that this ID shall be above 50 for custom DatasetImplementationTypes
		/// and unique across systems that can potentially deployed on one machine.
		/// Typically, this will need to be coordinated between the subclasses.</param>
		public DatasetImplementationType(int id)
		{
			Id = id;
		}

		static DatasetImplementationType()
		{
			Add(Table);
			Add(Vector);
			Add(Topology);
			Add(Raster);
			Add(RasterMosaic);
			Add(Terrain);
			Add(ErrorTable);
			Add(ErrorMultipoint);
			Add(ErrorLine);
			Add(ErrorPolygon);
			Add(ErrorMultiPatch);
		}

		#endregion

		public int Id { get; }

		[NotNull]
		public string Name => GetName();

		[CanBeNull]
		public static DatasetImplementationType Resolve(int id)
		{
			DatasetImplementationType role;
			return _types.TryGetValue(id, out role)
				       ? role
				       : null;
		}

		[NotNull]
		protected virtual string GetName()
		{
			return GetName(this);
		}

		[NotNull]
		protected static string GetName([NotNull] DatasetImplementationType implementationType)
		{
			if (_typeNames.TryGetValue(implementationType.Id, out string name))
			{
				return string.IsNullOrEmpty(name)
					       ? $"{implementationType.GetType().Name} id={implementationType.Id}"
					       : name;
			}

			// get all public static fields
			FieldInfo[] fieldInfos =
				implementationType.GetType().GetFields(BindingFlags.Public |
				                                       BindingFlags.Static);

			// make sure all implementation types are in the list
			foreach (FieldInfo fieldInfo in fieldInfos)
			{
				var type = fieldInfo.GetValue(implementationType) as DatasetImplementationType;

				if (type == null)
				{
					continue;
				}

				// the field is a DatasetImplementationType, make sure it is in the dictionary
				if (! _typeNames.TryGetValue(type.Id, out string _))
				{
					// don't use add to avoid threading issues
					_typeNames[type.Id] = $"{type.GetType().Name}.{fieldInfo.Name}";
				}
			}

			// try again after adding all
			_typeNames.TryGetValue(implementationType.Id, out name);

			return string.IsNullOrEmpty(name)
				       ? $"{implementationType.GetType().Name} id={implementationType.Id}"
				       : name;
		}

		#region Object overrides

		public override string ToString()
		{
			return Name;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			var implementationType = obj as DatasetImplementationType;
			if (implementationType == null)
			{
				return false;
			}

			return Id == implementationType.Id;
		}

		public override int GetHashCode()
		{
			return Id;
		}

		public static bool operator ==(DatasetImplementationType left,
		                               DatasetImplementationType right)
		{
			return left?.Id == right?.Id;
		}

		public static bool operator !=(DatasetImplementationType left,
		                               DatasetImplementationType right)
		{
			return left?.Id != right?.Id;
		}

		#endregion

		protected static void Add([NotNull] DatasetImplementationType role)
		{
			_types.Add(role.Id, role);
		}
	}
}
