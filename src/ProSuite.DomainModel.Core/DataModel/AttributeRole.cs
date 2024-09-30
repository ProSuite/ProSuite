using System.Collections.Generic;
using System.Reflection;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class AttributeRole
	{
		#region Static members

		public static readonly AttributeRole ObjectID = new AttributeRole(23);
		public static readonly AttributeRole UUID = new AttributeRole(1);
		public static readonly AttributeRole DateOfChange = new AttributeRole(2);
		public static readonly AttributeRole DateOfCreation = new AttributeRole(3);
		public static readonly AttributeRole Operator = new AttributeRole(4);
		public static readonly AttributeRole ObjectType = new AttributeRole(5);
		public static readonly AttributeRole Shape = new AttributeRole(30);
		public static readonly AttributeRole ShapeLength = new AttributeRole(31);
		public static readonly AttributeRole ShapeArea = new AttributeRole(32);

		public static readonly AttributeRole ObjectOrigin = new AttributeRole(6);
		public static readonly AttributeRole YearOfRevision = new AttributeRole(7);
		public static readonly AttributeRole MonthOfRevision = new AttributeRole(8);
		public static readonly AttributeRole YearOfOrigin = new AttributeRole(9);
		public static readonly AttributeRole MonthOfOrigin = new AttributeRole(10);
		public static readonly AttributeRole YearOfCreation = new AttributeRole(11);
		public static readonly AttributeRole MonthOfCreation = new AttributeRole(12);
		public static readonly AttributeRole ReasonForChange = new AttributeRole(13);

		public static readonly AttributeRole ErrorCorrectionStatus = new AttributeRole(14);
		public static readonly AttributeRole ErrorConditionName = new AttributeRole(15);

		public static readonly AttributeRole ErrorConditionParameters =
			new AttributeRole(16);

		public static readonly AttributeRole ErrorConditionId = new AttributeRole(17);
		public static readonly AttributeRole ErrorObjects = new AttributeRole(18);
		public static readonly AttributeRole ErrorDescription = new AttributeRole(21);
		public static readonly AttributeRole ErrorErrorType = new AttributeRole(22);
		public static readonly AttributeRole ErrorAffectedComponent = new AttributeRole(24);

		public static readonly AttributeRole ErrorQualityConditionVersion =
			new AttributeRole(25);

		// RSC renamed: from ErrorWorkUnitReference. One base role for wu references,
		// as these columns often have same rules associated. Differentiation in 
		// Topgis/Genius-specific roles still possible where needed.
		public static readonly AttributeRole WorkUnitReference = new AttributeRole(19);

		// TODO: IDs conflict with genius roles!!! -> coordinate
		public static readonly AttributeRole ConflictResolutionStatus =
			new AttributeRole(506);

		public static readonly AttributeRole ConflictRevisionStatus = new AttributeRole(507);

		public static readonly AttributeRole ConflictDescription = new AttributeRole(508);

		public static readonly AttributeRole ConflictResolutionTask = new AttributeRole(518);

		public static readonly AttributeRole ConflictObjectClass = new AttributeRole(509);
		public static readonly AttributeRole ConflictObjectUuid = new AttributeRole(510);
		public static readonly AttributeRole ConflictObjectId = new AttributeRole(511);

		public static readonly AttributeRole ConflictAncestorState = new AttributeRole(62);

		public static readonly AttributeRole ConflictPreReconcileState =
			new AttributeRole(63);

		public static readonly AttributeRole ConflictReconcileState = new AttributeRole(64);

		public static readonly AttributeRole RevisionPointFeatureClass =
			new AttributeRole(512);

		public static readonly AttributeRole RevisionPointStatus = new AttributeRole(513);
		public static readonly AttributeRole RevisionPointNote = new AttributeRole(514);
		public static readonly AttributeRole RevisionPointTopic = new AttributeRole(515);
		public static readonly AttributeRole RevisionPointOrigin = new AttributeRole(516);

		public static readonly AttributeRole RevisionPointFieldRelevant =
			new AttributeRole(517);

		public static readonly AttributeRole RevisionPointProduct =
			new AttributeRole(519);

		public static readonly AttributeRole EmailAddress = new AttributeRole(520);

		public static readonly AttributeRole ReleaseID = new AttributeRole(816);

		private static readonly Dictionary<int, AttributeRole> _roles =
			new Dictionary<int, AttributeRole>();

		private static readonly Dictionary<int, string> _roleNames =
			new Dictionary<int, string>();

		static AttributeRole()
		{
			Add(ObjectID);
			Add(UUID);
			Add(DateOfChange);
			Add(DateOfCreation);
			Add(Operator);
			Add(ObjectType);
			Add(Shape);
			Add(ShapeLength);
			Add(ShapeArea);

			Add(ObjectOrigin);
			Add(YearOfRevision);
			Add(MonthOfRevision);
			Add(YearOfOrigin);
			Add(MonthOfOrigin);
			Add(YearOfCreation);
			Add(MonthOfCreation);
			Add(ReasonForChange);

			Add(WorkUnitReference);

			Add(ErrorCorrectionStatus);
			Add(ErrorConditionName);
			Add(ErrorConditionParameters);
			Add(ErrorConditionId);
			Add(ErrorObjects);
			Add(ErrorDescription);
			Add(ErrorErrorType);
			Add(ErrorAffectedComponent);
			Add(ErrorQualityConditionVersion);

			Add(ConflictResolutionStatus);
			Add(ConflictRevisionStatus);
			Add(ConflictDescription);
			Add(ConflictResolutionTask);
			Add(ConflictObjectClass);
			Add(ConflictObjectUuid);
			Add(ConflictObjectId);

			Add(ConflictAncestorState);
			Add(ConflictPreReconcileState);
			Add(ConflictReconcileState);

			Add(RevisionPointFeatureClass);
			Add(RevisionPointStatus);
			Add(RevisionPointNote);
			Add(RevisionPointTopic);
			Add(RevisionPointOrigin);
			Add(RevisionPointFieldRelevant);
			Add(RevisionPointProduct);
			Add(EmailAddress);
		}

		[NotNull]
		public static AttributeRole Resolve(int id)
		{
			AttributeRole role;
			return _roles.TryGetValue(id, out role)
				       ? role
				       : new UnknownAttributeRole(id);
		}

		[NotNull]
		public static string GetName([NotNull] AttributeRole role)
		{
			string name;
			if (! _roleNames.TryGetValue(role.Id, out name))
			{
				// get all public static fields
				FieldInfo[] fieldInfos =
					role.GetType().GetFields(BindingFlags.Public |
					                         BindingFlags.Static);

				// make sure all roles of the passed type are in the list
				foreach (FieldInfo fieldInfo in fieldInfos)
				{
					var attributeRole = fieldInfo.GetValue(role) as AttributeRole;

					if (attributeRole == null)
					{
						continue;
					}

					// the field is an AttributeRole, make sure it is in the dictionary
					string existingName;
					if (! _roleNames.TryGetValue(attributeRole.Id, out existingName))
					{
						// don't use add to avoid threading issues
						_roleNames[attributeRole.Id] = string.Format(
							"{0}.{1}",
							attributeRole.GetType().Name,
							fieldInfo.Name);
					}
				}

				// try again after adding all
				_roleNames.TryGetValue(role.Id, out name);
			}

			return string.IsNullOrEmpty(name)
				       ? string.Format("{0} id={1}", role.GetType().Name, role.Id)
				       : name;
		}

		#endregion

		#region Fields

		private string _name;

		#endregion

		#region Constructors

		// don't make protected 
		// (R# is wrong; protected constructors can only be called from subclass *constructor*)
		public AttributeRole(int id)
		{
			Id = id;
		}

		#endregion

		public int Id { get; }

		[NotNull]
		public string Name
		{
			get
			{
				if (string.IsNullOrEmpty(_name))
				{
					_name = GetName();
				}

				return _name;
			}
		}

		[NotNull]
		protected virtual string GetName()
		{
			return GetName(this);
		}

		private static void Add([NotNull] AttributeRole role)
		{
			_roles.Add(role.Id, role);
		}

		#region Object overrides

		public override string ToString()
		{
			return Name;
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}

			var attributeRole = obj as AttributeRole;
			if (attributeRole == null)
			{
				return false;
			}

			return Id == attributeRole.Id;
		}

		public override int GetHashCode()
		{
			return Id;
		}

		// TODO: Consier making the attribute role a proper value type such as a struct / record
		// Some code uses the == comparison and expects it to work as value type comparison.
		public static bool operator ==(AttributeRole left, AttributeRole right)
		{
			return left?.Id == right?.Id;
		}

		public static bool operator !=(AttributeRole left, AttributeRole right)
		{
			return left?.Id != right?.Id;
		}

		#endregion
	}
}
