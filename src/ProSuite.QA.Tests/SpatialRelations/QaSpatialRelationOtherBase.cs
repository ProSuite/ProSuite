using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.SpatialRelations
{
	/// <summary>
	/// Check if there are any elements in two groups of layers that 
	/// have a certain relation with each other.
	/// This test often is used with constraints
	/// </summary>
	[CLSCompliant(false)]
	public abstract class QaSpatialRelationOtherBase : QaSpatialRelationBase
	{
		private readonly int _fromClassCount;

		#region Constructors

		protected QaSpatialRelationOtherBase(
			[NotNull] IList<IFeatureClass> featureClasses,
			[NotNull] IList<IFeatureClass> relatedClasses,
			esriSpatialRelEnum relation)
			: base(Union(featureClasses, relatedClasses), relation)
		{
			_fromClassCount = featureClasses.Count;
		}

		protected QaSpatialRelationOtherBase(
			[NotNull] IList<IFeatureClass> featureClasses,
			[NotNull] IList<IFeatureClass> relatedClasses,
			[NotNull] string intersectionMatrix)
			: base(Union(featureClasses, relatedClasses), intersectionMatrix)
		{
			_fromClassCount = featureClasses.Count;
		}

		protected QaSpatialRelationOtherBase([NotNull] IFeatureClass featureClass,
		                                     [NotNull] IFeatureClass relatedClass,
		                                     esriSpatialRelEnum relation)
			: this(new[] {featureClass}, new[] {relatedClass}, relation) { }

		protected QaSpatialRelationOtherBase([NotNull] IFeatureClass featureClass,
		                                     [NotNull] IFeatureClass relatedClass,
		                                     [NotNull] string intersectionMatrix)
			: this(new[] {featureClass}, new[] {relatedClass}, intersectionMatrix) { }

		#endregion

		public override bool ValidateParameters(out string error)
		{
			for (var i = 1; i < _fromClassCount; i++)
			{
				// IndexOf returns always the first Occurrence of an item
				if (InvolvedTables.IndexOf(InvolvedTables[i]) == i)
				{
					continue;
				}

				error = string.Format("{0} exists multiple times as from featureClass",
				                      DatasetUtils.GetName(InvolvedTables[i]));
				return false;
			}

			error = null;
			return true;
		}

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			Assert.ArgumentNotNull(row, nameof(row));

			var feature = row as IFeature;
			if (feature == null)
			{
				return NoError;
			}

			IGeometry shape = GetSearchGeometry(feature, tableIndex);

			if (shape == null || shape.IsEmpty)
			{
				return NoError;
			}

			var errorCount = 0;

			if (IsInFromTableList(tableIndex))
			{
				// the row is from one of the From tables - search in To tables
				for (int relatedTableIndex = _fromClassCount;
				     relatedTableIndex < TotalClassCount;
				     relatedTableIndex++)
				{
					errorCount += FindErrors(feature, shape, tableIndex, relatedTableIndex);
				}
			}
			else
			{
				// the row is from one of the To tables - search in From tables

				if (! IgnoreUndirected)
				{
					// test both ways
					for (var relatedTableIndex = 0;
					     relatedTableIndex < _fromClassCount;
					     relatedTableIndex++)
					{
						errorCount += FindErrors(feature, shape, tableIndex, relatedTableIndex);
					}
				}
			}

			return errorCount;
		}

		protected bool IsInFromTableList(int tableIndex)
		{
			return tableIndex < _fromClassCount;
		}

		protected override bool RequiresInvertedRelation(int tableIndex)
		{
			return IsInFromTableList(tableIndex);
		}

		private static IList<IFeatureClass> Union(
			params IList<IFeatureClass>[] featureClasses)
		{
			var union = new List<IFeatureClass>();

			foreach (IList<IFeatureClass> list in featureClasses)
			{
				union.AddRange(list);
			}

			return union;
		}

		[CanBeNull]
		protected virtual IGeometry GetSearchGeometry([NotNull] IFeature feature,
		                                              int tableIndex)
		{
			return feature.Shape;
		}
	}
}