using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Reflection;

namespace ProSuite.DomainModel.Core
{
	/// <summary>
	/// Describes a class (using type name and assembly name) and allows it to be instantiated.
	/// </summary>
	public class ClassDescriptor : IEquatable<ClassDescriptor>
	{
		[UsedImplicitly] private string _typeName;
		[UsedImplicitly] private string _assemblyName;
		[UsedImplicitly] private string _description;

		#region Constructors

		[UsedImplicitly]
		public ClassDescriptor() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ClassDescriptor"/> class.
		/// </summary>
		/// <param name="typeName">Name of the type.</param>
		/// <param name="assemblyName">Name of the assembly.</param>
		public ClassDescriptor(string typeName, string assemblyName) : this(
			typeName, assemblyName, string.Empty) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ClassDescriptor"/> class.
		/// </summary>
		/// <param name="typeName">Name of the type.</param>
		/// <param name="assemblyName">Name of the assembly.</param>
		/// <param name="description">The description.</param>
		public ClassDescriptor(string typeName, string assemblyName, string description)
		{
			_typeName = typeName;
			_assemblyName = assemblyName;
			_description = description;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ClassDescriptor"/> class.
		/// </summary>
		/// <param name="type">The type.</param>
		public ClassDescriptor([NotNull] Type type) : this(type, string.Empty) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ClassDescriptor"/> class.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="description">The description.</param>
		public ClassDescriptor([NotNull] Type type, [CanBeNull] string description)
			: this(type.FullName, type.Assembly.GetName().Name, description) { }

		#endregion

		public string TypeName
		{
			get { return _typeName; }
			set { _typeName = value; }
		}

		public string AssemblyName
		{
			get { return _assemblyName; }
			set { _assemblyName = value; }
		}

		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}

		[NotNull]
		public T CreateInstance<T>(params object[] args) where T : class
		{
			return (T) CreateInstance(args);
		}

		[NotNull]
		public object CreateInstance(params object[] args)
		{
			Type type = GetInstanceType();

			return args == null
				       ? Activator.CreateInstance(type)
				       : Activator.CreateInstance(type, args);
		}

		[NotNull]
		public Type GetInstanceType()
		{
			return PrivateAssemblyUtils.LoadType(_assemblyName, _typeName);
		}

		public bool HasEqualType([CanBeNull] ClassDescriptor other)
		{
			if (this == other)
			{
				return true;
			}

			if (other == null)
			{
				return false;
			}

			return
				Equals(_typeName, other._typeName) &&
				Equals(_assemblyName, other._assemblyName);
		}

		#region Object overrides

		public bool Equals(ClassDescriptor other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Equals(other._typeName, _typeName) &&
			       Equals(other._assemblyName, _assemblyName);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != typeof(ClassDescriptor))
			{
				return false;
			}

			return Equals((ClassDescriptor) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((_typeName != null
					         ? _typeName.GetHashCode()
					         : 0) * 397) ^ (_assemblyName != null
						                        ? _assemblyName.GetHashCode()
						                        : 0);
			}
		}

		public override string ToString()
		{
			return _typeName ?? "<no type name>";
		}

		#endregion
	}
}
