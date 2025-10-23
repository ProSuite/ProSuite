using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.QA.Tests.Coincidence
{
	public class ExpressionBasedDistanceProviderDefinition
	{
		[NotNull]
		public ICollection<string> Expressions { get; }

		[NotNull]
		public ICollection<IFeatureClassSchemaDef> FeatureClasses { get; }

		public ExpressionBasedDistanceProviderDefinition(
			[NotNull] ICollection<string> expressions,
			[NotNull] ICollection<IFeatureClassSchemaDef> featureClasses)
		{
			Expressions = expressions;
			FeatureClasses = featureClasses;
		}
	}
}
