using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TableBased;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA
{
	public class ObjectSelection : IObjectSelection
	{
		[NotNull] private readonly Dictionary<IObjectDataset, HashSet<long>> _objectsByDataset =
			new Dictionary<IObjectDataset, HashSet<long>>();

		[NotNull] private readonly IQualityConditionObjectDatasetResolver _datasetResolver;

		public ObjectSelection(
			[NotNull] IEnumerable<IObject> selectedObjects,
			[NotNull] IDatasetLookup datasetLookup,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver)
		{
			Assert.ArgumentNotNull(selectedObjects, nameof(selectedObjects));
			Assert.ArgumentNotNull(datasetLookup, nameof(datasetLookup));
			Assert.ArgumentNotNull(datasetResolver, nameof(datasetResolver));

			_datasetResolver = datasetResolver;

			ICollection<IObject> objectCollection =
				CollectionUtils.GetCollection(selectedObjects);

			IDictionary<IObjectClass, IObjectDataset> datasetsByClass =
				VerificationUtils.GetDatasetsByObjectClass(
					objectCollection, datasetLookup);

			foreach (IObject obj in objectCollection)
			{
				if (! obj.HasOID)
				{
					continue;
				}

				IObjectClass objectClass = obj.Class;
				IObjectDataset dataset = datasetsByClass[objectClass];

				Assert.NotNull(dataset, "Unable to resolve dataset for object class {0}",
				               DatasetUtils.GetName(objectClass));

				HashSet<long> objectIds;
				if (! _objectsByDataset.TryGetValue(dataset, out objectIds))
				{
					objectIds = new HashSet<long>();
					_objectsByDataset.Add(dataset, objectIds);
				}

				objectIds.Add(obj.OID);
			}
		}

		public IEnumerable<long> GetSelectedOIDs(IObjectDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			HashSet<long> objectIds;
			return _objectsByDataset.TryGetValue(dataset, out objectIds)
				       ? objectIds
				       : (IEnumerable<long>) new long[] { };
		}

		public bool Contains(InvolvedRow involvedRow, QualityCondition qualityCondition)
		{
			return Contains(involvedRow, qualityCondition, out bool _);
		}

		public bool Contains(InvolvedRow involvedRow, QualityCondition qualityCondition,
		                     out bool unknownTable)
		{
			if (involvedRow.RepresentsEntireTable)
			{
				unknownTable = false; // does not matter
				return false;
			}

			IObjectDataset dataset = _datasetResolver.GetDatasetByInvolvedRowTableName(
				involvedRow.TableName, qualityCondition);

			if (dataset == null)
			{
				// unable to resolve the dataset 
				unknownTable = true;
				return false;
			}

			unknownTable = false;
			HashSet<long> objectIds;
			return _objectsByDataset.TryGetValue(dataset, out objectIds) &&
			       objectIds.Contains(involvedRow.OID);
		}
	}
}
