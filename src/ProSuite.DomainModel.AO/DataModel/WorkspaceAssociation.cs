using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public class WorkspaceAssociation
	{
		public WorkspaceAssociation([NotNull] string relationshipClassName,
		                            [CanBeNull] string featureDatasetName,
		                            [NotNull] Association association)
		{
			Assert.ArgumentNotNullOrEmpty(relationshipClassName,
			                              nameof(relationshipClassName));
			Assert.ArgumentNotNull(association, nameof(association));

			RelationshipClassName = relationshipClassName;
			FeatureDatasetName = featureDatasetName;
			Association = association;
		}

		[NotNull]
		public string RelationshipClassName { get; }

		[CanBeNull]
		[PublicAPI]
		public string FeatureDatasetName { get; }

		[NotNull]
		public Association Association { get; }
	}
}
