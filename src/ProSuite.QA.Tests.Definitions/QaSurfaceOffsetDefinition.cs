using System;
using System.Collections.Generic;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests
{
	public abstract class QaSurfaceOffsetDefinition : AlgorithmDefinition
	{
		protected QaSurfaceOffsetDefinition([NotNull] IFeatureClassSchemaDef featureClass,
		                                    [NotNull] ITerrainDef terrain,
		                                    double terrainTolerance,
		                                    double limit,
		                                    ZOffsetConstraint zOffsetConstraint)
			: this(featureClass, limit, zOffsetConstraint)
		{
			Assert.ArgumentNotNull(terrain, nameof(terrain));

			InvolvedTerrains = new List<ITerrainDef> { terrain };
			TerrainTolerance = terrainTolerance;
		}

		public List<ITerrainDef> InvolvedTerrains { get; set; }
		public List<IRasterDatasetDef> InvolvedRasters { get; set; }

		public double TerrainTolerance { get; set; }

		public double Limit { get; }

		public ZOffsetConstraint ZOffsetConstraint { get; }

		protected QaSurfaceOffsetDefinition([NotNull] IFeatureClassSchemaDef featureClass,
		                                    [NotNull] IRasterDatasetDef rasterReference,
		                                    double limit,
		                                    ZOffsetConstraint zOffsetConstraint)
			: this(featureClass, limit, zOffsetConstraint)
		{
			Assert.ArgumentNotNull(rasterReference, nameof(rasterReference));

			InvolvedRasters = new List<IRasterDatasetDef> { rasterReference };
		}

		private QaSurfaceOffsetDefinition([NotNull] IFeatureClassSchemaDef featureClass,
		                                  double limit,
		                                  ZOffsetConstraint zOffsetConstraint)
			: base(featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			if ((zOffsetConstraint == ZOffsetConstraint.WithinLimit ||
			     zOffsetConstraint == ZOffsetConstraint.OutsideLimit) &&
			    limit < 0)
			{
				throw new ArgumentOutOfRangeException(
					nameof(zOffsetConstraint), zOffsetConstraint,
					$@"Limit {limit} < 0 not allowed for Constraint {zOffsetConstraint}");
			}

			Limit = limit;
			ZOffsetConstraint = zOffsetConstraint;
		}

		protected ErrorType GetErrorType(double dist, ref double max)
		{
			if (double.IsNaN(dist))
			{
				return ErrorType.NoTerrain;
			}

			switch (ZOffsetConstraint)
			{
				case ZOffsetConstraint.AboveLimit:
					if (dist < Limit)
					{
						if (double.IsNaN(max) || dist < max)
						{
							max = dist;
						}

						return ErrorType.TooSmall;
					}

					break;

				case ZOffsetConstraint.BelowLimit:
					if (dist > Limit)
					{
						if (double.IsNaN(max) || dist > max)
						{
							max = dist;
						}

						return ErrorType.TooLarge;
					}

					break;

				case ZOffsetConstraint.WithinLimit:
					if (Math.Abs(dist) > Limit)
					{
						if (double.IsNaN(max) || Math.Abs(dist) > Math.Abs(max))
						{
							max = dist;
						}

						return ErrorType.TooLarge;
					}

					break;

				case ZOffsetConstraint.OutsideLimit:
					if (Math.Abs(dist) < Limit)
					{
						if (double.IsNaN(max) || Math.Abs(dist) < Math.Abs(max))
						{
							max = dist;
						}

						return ErrorType.TooSmall;
					}

					break;

				default:
					throw new NotImplementedException("Unhandled ZOffsetConstraint " +
					                                  ZOffsetConstraint);
			}

			return ErrorType.None;
		}

		#region Nested type: ErrorType

		protected enum ErrorType
		{
			None,
			NoTerrain,
			TooSmall,
			TooLarge
		}

		#endregion
	}
}
