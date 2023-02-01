using System.ComponentModel;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.SchemaInfo
{
	public abstract class ObjectClassProperties
	{
		protected ObjectClassProperties([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			Workspace = new WorkspaceProperties(DatasetUtils.GetWorkspace(objectClass));
			AliasName = DatasetUtils.GetAliasName(objectClass);
			Name = DatasetUtils.GetName(objectClass);
			SubtypeField = GetSubtypeFieldText(objectClass);

			var versionedObject = objectClass as IVersionedObject;

			RegisteredAsVersioned = versionedObject != null &&
			                        versionedObject.IsRegisteredAsVersioned;
		}

		[Category(PropertyCategories.General)]
		[UsedImplicitly]
		public string Name { get; private set; }

		[Category(PropertyCategories.General)]
		[DisplayName("Alias Name")]
		[UsedImplicitly]
		public string AliasName { get; private set; }

		[Category(PropertyCategories.General)]
		[DisplayName("Subtype Field")]
		[UsedImplicitly]
		public string SubtypeField { get; private set; }

		[Category(PropertyCategories.General)]
		[TypeConverter(typeof(AllPropertiesConverter))]
		[UsedImplicitly]
		public WorkspaceProperties Workspace { get; private set; }

		[Category(PropertyCategories.General)]
		[DisplayName("Registered as Versioned")]
		[UsedImplicitly]
		public bool RegisteredAsVersioned { get; private set; }

		[NotNull]
		private static string GetSubtypeFieldText([NotNull] IObjectClass objectClass)
		{
			int subtypeFieldIndex = DatasetUtils.GetSubtypeFieldIndex(objectClass);
			if (subtypeFieldIndex < 0)
			{
				return "No subtype field";
			}

			IField field = objectClass.Fields.Field[subtypeFieldIndex];
			return field.Name; // use alias also?
		}
	}
}
