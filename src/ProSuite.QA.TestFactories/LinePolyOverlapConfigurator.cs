using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.QA.TestFactories
{
	public class LineNotNearPolyOverlapConfigurator
	{
		/// <summary>
		/// start index for reference subtypes in the ';'-separated list of rules for a feature subtype
		/// prefields: SubtypeCode; DefaultNear; RightSideNear;
		/// </summary>
		public const int ReferenceSubtypesStart = 3;

		private const string _importedQualityCondition = "<imported_quality_condition>";
		private const string _importedTestDescriptor = "<imported_test_descriptor>";

		[NotNull]
		public QualityCondition Convert([NotNull] Matrix matrix,
		                                [NotNull] IList<Dataset> datasets)
		{
			Assert.ArgumentNotNull(matrix, nameof(matrix));
			Assert.ArgumentNotNull(datasets, nameof(datasets));

			var classDescriptor = new ClassDescriptor(typeof(QaTopoNotNearPolyFactory));

			var testDescriptor = new TestDescriptor(
				_importedTestDescriptor, classDescriptor);

			var qualityCondition = new QualityCondition(
				_importedQualityCondition, testDescriptor);

			Dictionary<string, TestParameter> parDict = QaTopoNotNearPolyFactory
			                                            .CreateParameterList()
			                                            .ToDictionary(x => x.Name);

			VectorDataset featureClassDs = GetDataset(matrix.FeatureClassName, datasets);
			qualityCondition.AddParameterValue(new DatasetTestParameterValue(
				                                   parDict[
					                                   QaTopoNotNearPolyFactory
						                                   .FeatureClassParamName], featureClassDs)
			                                   {FilterExpression = matrix.FeatureClassFilter});
			VectorDataset referenceDs = GetDataset(matrix.ReferenceName, datasets);
			qualityCondition.AddParameterValue(new DatasetTestParameterValue(
				                                   parDict[
					                                   QaTopoNotNearPolyFactory.ReferenceParamName],
				                                   referenceDs)
			                                   {FilterExpression = matrix.ReferenceFilter});

			{
				GetSubtypes(featureClassDs, out string field, out IList<Subtype> subtypes);
				Assert.NotNull(field);
				Assert.NotNull(subtypes);

				foreach (FeatureTypeProps featureTypeProps in matrix.FeatureClassTypes)
				{
					string subtypeName = featureTypeProps.SubType.SubtypeName.Trim();
					Subtype subtype = subtypes.First(x => x.Name.Equals(subtypeName));

					StringBuilder rulesBuilder = new StringBuilder();
					rulesBuilder.Append($"{subtype.Code};");
					rulesBuilder.Append($"{featureTypeProps.DefaultDistance};");
					rulesBuilder.Append($"{featureTypeProps.RightSideDistance};");
					foreach (int canOverlap in featureTypeProps.CanOverlap)
					{
						rulesBuilder.Append($"{canOverlap};");
					}

					qualityCondition.AddParameterValue(new ScalarTestParameterValue(
						                                   parDict[
							                                   QaTopoNotNearPolyFactory
								                                   .FeaturesubtypeRulesParamName],
						                                   rulesBuilder.ToString()));
				}
			}
			{
				GetSubtypes(referenceDs, out string field, out IList<Subtype> subtypes);
				Assert.NotNull(field);
				Assert.NotNull(subtypes);

				foreach (ConnectionType refType in matrix.ReferenceTypes)
				{
					string subtypeName = refType.SubtypeName.Trim();
					Subtype subtype = subtypes.First(x => x.Name.Equals(subtypeName));
					qualityCondition.AddParameterValue(new ScalarTestParameterValue(
						                                   parDict[
							                                   QaTopoNotNearPolyFactory
								                                   .ReferenceSubtypesParamName],
						                                   subtype.Code));
				}
			}

			return qualityCondition;
		}

		[NotNull]
		public static Matrix Convert([NotNull] QualityCondition qualityCondition)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

			IList<TestParameterValue> featureClassParameters =
				qualityCondition.GetParameterValues(QaTopoNotNearPolyFactory.FeatureClassParamName);
			Assert.AreEqual(1, featureClassParameters.Count,
			                $"found {featureClassParameters.Count} featureClass, expected 1");

			IList<TestParameterValue> referenceParameters =
				qualityCondition.GetParameterValues(QaTopoNotNearPolyFactory.ReferenceParamName);
			Assert.AreEqual(1, referenceParameters.Count,
			                $"found {referenceParameters.Count} near, expected 1");

			VectorDataset featureClassDataset = GetVectorDataset(featureClassParameters[0]);
			VectorDataset referenceDataset = GetVectorDataset(referenceParameters[0]);

			IList<TestParameterValue> referenceSubtypesParameters =
				qualityCondition.GetParameterValues(
					QaTopoNotNearPolyFactory.ReferenceSubtypesParamName);
			IList<int> referenceSubtypes =
				referenceSubtypesParameters.Select(x => int.Parse(x.StringValue)).ToList();

			IList<TestParameterValue> featuresubtypeRulesParameters =
				qualityCondition.GetParameterValues(
					QaTopoNotNearPolyFactory.FeaturesubtypeRulesParamName);
			IList<string> featuresubtypeRules =
				featuresubtypeRulesParameters.Select(x => x.StringValue).ToList();

			Matrix matrix = InitMatrix(
				featureClassDataset,
				((DatasetTestParameterValue) featureClassParameters[0]).FilterExpression,
				referenceDataset,
				((DatasetTestParameterValue) referenceParameters[0]).FilterExpression,
				referenceSubtypes, featuresubtypeRules);

			return matrix;
		}

		#region Non-public

		private static void GetSubtypes([NotNull] IObjectDataset objectDataset,
		                                [CanBeNull] out string field,
		                                [CanBeNull] out IList<Subtype> subtypes)
		{
			IObjectClass objectClass = ConfiguratorUtils.OpenFromDefaultDatabase(objectDataset);
			var s = (ISubtypes) objectClass;
			field = s.SubtypeFieldName;

			subtypes = string.IsNullOrEmpty(field)
				           ? null
				           : DatasetUtils.GetSubtypes(objectClass);
		}

		[NotNull]
		private static VectorDataset GetVectorDataset(
			[NotNull] TestParameterValue featureClassParameter)
		{
			var datasetParameterValue =
				(DatasetTestParameterValue) featureClassParameter;
			var dataset = (VectorDataset) datasetParameterValue.DatasetValue;

			Assert.NotNull(dataset, "Dataset parameter {0} does not refer to a dataset",
			               datasetParameterValue.TestParameterName);

			return dataset;
		}

		[NotNull]
		private static Matrix InitMatrix(
			[NotNull] VectorDataset featureClassDataset,
			[CanBeNull] string featureClassFilter,
			[NotNull] VectorDataset referenceDataset,
			[CanBeNull] string referenceFilter,
			[NotNull] IList<int> referenceSubtypes,
			[NotNull] IList<string> featuresubtypeRules)
		{
			List<ConnectionType> refSubtypes = new List<ConnectionType>();
			{
				GetSubtypes(referenceDataset, out string field, out IList<Subtype> refTypes);
				Assert.NotNull(refTypes);

				string reference = referenceDataset.Name;
				foreach (int referenceSubtype in referenceSubtypes)
				{
					string subtype = refTypes.First(x => x.Code == referenceSubtype).Name;
					refSubtypes.Add(
						new ConnectionType(reference, field, subtype, referenceSubtype)
						{FeatureClassFilter = referenceFilter});
				}

				if (refSubtypes.Count == 0)
				{
					foreach (Subtype refType in refTypes)
					{
						refSubtypes.Add(
							new ConnectionType(reference, field, refType.Name, refType.Code)
							{FeatureClassFilter = referenceFilter});
					}
				}
			}

			List<FeatureTypeProps> featureClassSubtypes = new List<FeatureTypeProps>();
			{
				GetSubtypes(featureClassDataset, out string field, out IList<Subtype> fcTypes);
				Assert.NotNull(fcTypes);

				string featureclass = featureClassDataset.Name;
				foreach (string rule in featuresubtypeRules)
				{
					IList<string> ruleParts = rule.Split(';');
					int fcSubtype = int.Parse(ruleParts[0]);
					string subtype = fcTypes.First(x => x.Code == fcSubtype).Name;
					List<int> canOverlaps = new List<int>();
					foreach (int overlapIndex in EnumOverlapIndices(
						         ruleParts, rule, referenceSubtypes))
					{
						if (! int.TryParse(ruleParts[overlapIndex], out int canOverlap))
						{
							throw new InvalidOperationException(
								$"Invalid overlap index '{ruleParts[overlapIndex]}' in featureSubtypeRule '{rule}'");
						}

						canOverlaps.Add(canOverlap);
					}

					ConnectionType conn =
						new ConnectionType(featureclass, field, subtype, fcSubtype)
						{FeatureClassFilter = featureClassFilter};
					FeatureTypeProps props =
						new FeatureTypeProps
						{
							SubType = conn,
							DefaultDistance = double.Parse(ruleParts[1]),
							CanOverlap = canOverlaps
						};
					if (! string.IsNullOrWhiteSpace(ruleParts[2]))
					{
						props.RightSideDistance = double.Parse(ruleParts[2]);
					}

					featureClassSubtypes.Add(props);
				}

				if (featuresubtypeRules.Count == 0)
				{
					foreach (Subtype fcType in fcTypes)
					{
						ConnectionType conn =
							new ConnectionType(featureclass, field, fcType.Name, fcType.Code)
							{FeatureClassFilter = featureClassFilter};
						featureClassSubtypes.Add(new FeatureTypeProps
						                         {
							                         SubType = conn, DefaultDistance = 1,
							                         CanOverlap =
								                         new List<int>(new int[refSubtypes.Count])
						                         });
					}
				}
			}
			return new Matrix(featureClassSubtypes, refSubtypes);
		}

		public static IEnumerable<int> EnumOverlapIndices(IList<string> ruleParts,
		                                                  string featureClassRule,
		                                                  IList<int> referenceSubtypes)
		{
			int rulePartsLength = string.IsNullOrWhiteSpace(ruleParts[ruleParts.Count - 1])
				                      ? ruleParts.Count - 1
				                      : ruleParts.Count;
			if (rulePartsLength != ReferenceSubtypesStart +
			    referenceSubtypes.Count)
			{
				throw new InvalidOperationException(
					$"Unexpected number of rules in featureClassRule '{featureClassRule}' ");
			}

			for (int iRefSubtype = ReferenceSubtypesStart;
			     iRefSubtype < rulePartsLength;
			     iRefSubtype++)
			{
				yield return iRefSubtype;
			}
		}

		[NotNull]
		private static VectorDataset GetDataset(
			[NotNull] string datasetName,
			[NotNull] IEnumerable<Dataset> datasets)
		{
			var dataset = ConfiguratorUtils.GetDataset<VectorDataset>(datasetName,
				datasets);
			Assert.NotNull(dataset, "Vector dataset not found: {0}", datasetName);
			return dataset;
		}

		#endregion

		#region nested classes

		#region Nested type: ConnectionType

		public class ConnectionType
		{
			private readonly string _featureClassName;
			private readonly int _subtypeCode = -1;
			private readonly string _subtypeField;
			private readonly string _subtypeName;

			public ConnectionType([NotNull] string featureClassName, string subtypeName)
			{
				_featureClassName = featureClassName;
				_subtypeName = subtypeName;
			}

			public ConnectionType([NotNull] string featureClassName, int code)
			{
				_featureClassName = featureClassName;
				_subtypeCode = code;
			}

			public ConnectionType([NotNull] string featureClassName,
			                      string field, string subtype, int code)
			{
				_featureClassName = featureClassName;
				_subtypeCode = code;

				_subtypeField = field;
				_subtypeName = subtype;
			}

			[NotNull]
			public string FeatureClassName => _featureClassName;

			public string FeatureClassFilter { get; set; }

			public string SubtypeField => _subtypeField;

			public string SubtypeName => _subtypeName;

			public int SubtypeCode => _subtypeCode;
		}

		public class FeatureTypeProps
		{
			public ConnectionType SubType { get; set; }
			public double DefaultDistance { get; set; }
			public double? RightSideDistance { get; set; }
			public List<int> CanOverlap { get; set; }
		}

		#endregion

		#region Nested type: Matrix

		public class Matrix
		{
			private const int
				_matStart =
					4; // prefields: FeatureClassName; FeatureClassSubtype; DefaultNear; RightSideNear;

			private List<FeatureTypeProps> _featureClassTypes;
			private List<ConnectionType> _referenceTypes;

			private Matrix() { }

			internal Matrix([NotNull] List<FeatureTypeProps> featureclassTypes,
			                List<ConnectionType> referenceTypes)
			{
				_featureClassTypes = featureclassTypes;
				_referenceTypes = referenceTypes;
			}

			public IList<FeatureTypeProps> FeatureClassTypes => _featureClassTypes;
			public IList<ConnectionType> ReferenceTypes => _referenceTypes;

			public string FeatureClassName
			{
				get
				{
					string fcName = null;
					foreach (FeatureTypeProps prop in _featureClassTypes)
					{
						if (fcName == null)
						{
							fcName = prop.SubType.FeatureClassName;
						}
						else
						{
							Assert.AreEqual(
								fcName, prop.SubType.FeatureClassName,
								$"Found differing FeatureClasses: {fcName}, {prop.SubType.FeatureClassName} ");
						}
					}

					return fcName;
				}
			}

			public string FeatureClassFilter
			{
				get
				{
					foreach (FeatureTypeProps prop in _featureClassTypes)
					{
						return prop.SubType.FeatureClassFilter;
					}

					return null;
				}
			}

			public string ReferenceName
			{
				get
				{
					string refName = null;
					foreach (ConnectionType conn in _referenceTypes)
					{
						if (refName == null)
						{
							refName = conn.FeatureClassName;
						}
						else
						{
							Assert.AreEqual(
								refName, conn.FeatureClassName,
								$"Found differing Reference FeatureClasses: {refName}, {conn.FeatureClassName} ");
						}
					}

					return refName;
				}
			}

			public string ReferenceFilter
			{
				get
				{
					foreach (var prop in _referenceTypes)
					{
						return prop.FeatureClassFilter;
					}

					return null;
				}
			}

			[NotNull]
			public static Matrix Create([NotNull] TextReader textReader)
			{
				var m = new Matrix();

				string referenceClassesLine = textReader.ReadLine();
				string referenceSubtypesLine = textReader.ReadLine();

				Assert.NotNull(referenceClassesLine, nameof(referenceClassesLine));
				Assert.NotNull(referenceSubtypesLine, nameof(referenceSubtypesLine));

				m._referenceTypes = new List<ConnectionType>();

				string[] referenceNames = referenceClassesLine.Split(';');
				string[] referenceSubTypeStrings = referenceSubtypesLine.Split(';');

				for (int referenceIndex = _matStart;
				     referenceIndex < referenceNames.Length;
				     referenceIndex++)
				{
					string referenceName = referenceNames[_matStart].Trim();
					string referenceFilter = referenceNames[_matStart + 1].Trim();
					string referenceSubType = referenceSubTypeStrings[referenceIndex].Trim();

					if (string.IsNullOrEmpty(referenceSubType)
					    && referenceIndex == referenceNames.Length - 1)
					{
						// ; at end of line
						break;
					}

					Assert.NotNullOrEmpty(referenceName, nameof(referenceName));

					m._referenceTypes.Add(new ConnectionType(referenceName, referenceSubType)
					                      {FeatureClassFilter = referenceFilter,});
				}

				GetFeatureClassTypes(textReader, m);

				return m;
			}

			private static bool GetFeatureClassTypes([NotNull] TextReader textReader,
			                                         [NotNull] Matrix matrix)
			{
				string featureClassName = null;
				string featureClassFilter = null;
				int iRow = 0;
				while (true)
				{
					string matLine = textReader.ReadLine();
					if (string.IsNullOrEmpty(matLine))
					{
						foreach (FeatureTypeProps fcProps in matrix._featureClassTypes)
						{
							fcProps.SubType.FeatureClassFilter = featureClassFilter;
						}

						return false;
					}

					string[] matStrings = matLine.Split(';');
					string defString = matStrings[0].Trim();
					string featureClassSubtype = matStrings[1].Trim();
					string defaultDistance = matStrings[2];
					string rightSideDistance = matStrings[3];

					if (iRow == 0)
					{
						featureClassName = defString;
					}

					if (iRow == 1)
					{
						featureClassFilter = defString;
					}

					iRow++;

					FeatureTypeProps props =
						new FeatureTypeProps
						{
							SubType = new ConnectionType(Assert.NotNull(featureClassName),
							                             featureClassSubtype)
						};
					props.CanOverlap = new List<int>();
					props.DefaultDistance = double.Parse(defaultDistance);
					if (! string.IsNullOrWhiteSpace(rightSideDistance))
					{
						props.RightSideDistance = double.Parse(rightSideDistance);
					}

					// TODO: verify size
					for (int iRefType = 0; iRefType < matrix._referenceTypes.Count; iRefType++)
					{
						string overlapString = matStrings[_matStart + iRefType].Trim();
						if (overlapString == "x")
							overlapString = "-1";

						props.CanOverlap.Add(int.Parse(overlapString));
					}

					matrix._featureClassTypes =
						matrix._featureClassTypes ?? new List<FeatureTypeProps>();
					matrix._featureClassTypes.Add(props);
				}
			}

			[NotNull]
			public string ToCsv()
			{
				var sb = new StringBuilder();

				// 1. line
				sb.Append(";;(0: overlaps allowed);(x: no overlaps allowed);");
				sb.Append(
					$"{_referenceTypes[0].FeatureClassName};{_referenceTypes[0].FeatureClassFilter}");
				for (int iRefType = 1; iRefType < _referenceTypes.Count; iRefType++)
				{
					sb.Append(";");
				}

				sb.AppendLine();

				// 2. line
				sb.Append(";;(default near);(right side near);");
				foreach (ConnectionType referenceType in _referenceTypes)
				{
					sb.Append($"{referenceType.SubtypeName};");
				}

				sb.AppendLine();

				// featureclass subtypes
				int iRow = 0;
				foreach (FeatureTypeProps props in _featureClassTypes)
				{
					if (iRow == 0)
					{
						sb.Append(props.SubType.FeatureClassName);
					}

					if (iRow == 1)
					{
						sb.Append(props.SubType.FeatureClassFilter);
					}

					iRow++;

					sb.Append($";{props.SubType.SubtypeName};");
					sb.Append(
						$"{props.DefaultDistance};{props.RightSideDistance?.ToString() ?? ""};");
					foreach (int canOverlap in props.CanOverlap)
					{
						string s = canOverlap == 0 ? "0" : "x";
						sb.Append($"{s};");
					}

					sb.AppendLine();
				}

				return sb.ToString();
			}
		}

		#endregion

		#endregion
	}
}
