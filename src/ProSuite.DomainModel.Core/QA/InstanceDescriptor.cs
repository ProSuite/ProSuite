using System;
using System.Reflection;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Reflection;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.QA
{
	/// <summary>
	/// Base instance descriptor entity for TestDescriptors, RowFilterDescriptors,
	/// IssueFilterDescriptors and TransformerDescriptors.
	/// </summary>
	public abstract class InstanceDescriptor : EntityWithMetadata, INamed, IAnnotated
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private int _cloneId = -1;

		[UsedImplicitly] private string _name;
		[UsedImplicitly] private string _description;

		[UsedImplicitly] private ClassDescriptor _class;
		[UsedImplicitly] private int _constructorId = -1;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceDescriptor"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected InstanceDescriptor() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceDescriptor"/> class.
		/// </summary>
		protected InstanceDescriptor([NotNull] string name,
		                             [CanBeNull] string description = null)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			_name = name;
			_description = description;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceDescriptor"/> class.
		/// </summary>
		protected InstanceDescriptor([NotNull] string name,
		                             [NotNull] ClassDescriptor classDescriptor,
		                             int constructorId,
		                             [CanBeNull] string description = null)
			: this(name, description)
		{
			Assert.ArgumentNotNull(classDescriptor, nameof(classDescriptor));

			_class = classDescriptor;
			_constructorId = constructorId;
		}

		#endregion

		#region INamed Members

		[Required]
		[MaximumStringLength(200)]
		[ValidName]
		public string Name
		{
			get => _name;
			set => _name = value;
		}

		public abstract string TypeDisplayName { get; }

		#endregion

		#region IAnnotated Members

		public string Description
		{
			get => _description;
			set => _description = value;
		}

		#endregion

		public int ConstructorId
		{
			get => _constructorId;
			set
			{
				if (value == _constructorId)
				{
					return;
				}

				_constructorId = value;

				OnSetConstructorId();
			}
		}

		[CanBeNull]
		public ClassDescriptor Class
		{
			get => _class;
			set
			{
				if (value == _class)
				{
					return;
				}

				_class = value;
				if (value == null)
				{
					_constructorId = -1;
				}
				else if (_constructorId < 0)
				{
					// Only set the constructor if it has not already been assigned (e.g. by persistence)
					try
					{
						_constructorId = GetDefaultConstructorId(_class);
					}
					catch (Exception e)
					{
						_msg.WarnFormat("Error determining default constructor id: {0}", e.Message);
					}

					OnSetClass();
				}
			}
		}

		/// <summary>
		/// Gets the name of the assembly which contains the implementation.
		/// </summary>
		/// <value>The name of the assembly.</value>
		public virtual string TestAssemblyName => Class?.AssemblyName;

		public new int Id
		{
			get
			{
				if (base.Id < 0 && _cloneId != -1)
				{
					return _cloneId;
				}

				return base.Id;
			}
		}

		/// <summary>
		/// The clone Id can be set if this instance is a (remote) clone of a persistent instance descriptor.
		/// </summary>
		/// <param name="id"></param>
		public void SetCloneId(int id)
		{
			Assert.True(base.Id < 0, "Persistent entity or already initialized clone.");
			_cloneId = id;
		}

		public abstract InstanceConfiguration CreateConfiguration();

		/// <summary>
		/// Gets the canonical name of the instance descriptor, i.e. className(constructorIndex)
		/// or factoryClassName for test factories. 
		/// </summary>
		/// <returns></returns>
		public abstract string GetCanonicalName();

		#region Equality members

		public virtual bool Equals(InstanceDescriptor other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Equals(other._name, _name) &&
			       other._constructorId == _constructorId &&
			       Equals(other._class, _class);
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

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((InstanceDescriptor) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = (_name != null
					              ? _name.GetHashCode()
					              : 0);
				result = (result * 397) ^ (_class != null
					                           ? _class.GetHashCode()
					                           : 0);
				result = (result * 397) ^ _constructorId;

				return result;
			}
		}

		#endregion

		#region Non-public members

		protected virtual void OnSetClass() { }

		protected virtual void OnSetConstructorId() { }

		protected static int GetDefaultConstructorId([NotNull] ClassDescriptor classDescriptor)
		{
			Assert.ArgumentNotNull(classDescriptor, nameof(classDescriptor));

			Type classType = classDescriptor.GetInstanceType();

			var constructorIndex = 0;
			foreach (ConstructorInfo ctorInfo in classType.GetConstructors())
			{
				// return the first non-obsolete
				if (! ReflectionUtils.IsObsolete(ctorInfo))
				{
					return constructorIndex;
				}

				constructorIndex++;
			}

			// if there is no non-obsolete constructor, just return the first
			return 0;
		}

		protected static bool AreEqual(ClassDescriptor x, ClassDescriptor y)
		{
			if ((x == null) != (y == null))
			{
				return false;
			}

			if (x == null)
			{
				Assert.Null(y, "Invalid program");
				return true;
			}

			return x.HasEqualType(y);
		}

		#endregion
	}
}
