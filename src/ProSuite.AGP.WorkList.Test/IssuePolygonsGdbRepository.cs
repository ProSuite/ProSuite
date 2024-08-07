using System;
using System.Collections.Generic;
using System.Globalization;
using ArcGIS.Core.Data;
using NUnit.Framework;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.AGP.Storage;

namespace ProSuite.AGP.WorkList.Test
{
	public class IssuePolygonsGdbRepository : GdbRepository<IssueItem, FeatureClass>
	{
		private readonly IList<GdbRowIdentity> _issueStates;
		private readonly string _issueStatePath;

		private AttributeReader _attributeReader {
			get
			{
				return new AttributeReader(GdbTableDefinition as TableDefinition,
				                           Attributes.QualityConditionName,
				                           Attributes.InvolvedObjects,
				                           Attributes.IssueSeverity,
				                           Attributes.IssueCode
				);
			}
		}

		public IssuePolygonsGdbRepository(string gdbPath, string className = null) : base(gdbPath, className)
		{
		}

		public IssuePolygonsGdbRepository(IssueWorkListDefinition workListDef, string className = null) : base(workListDef.FgdbPath, className)
		{
			// open XML repository for IssueWorklistDefinition?
			_issueStates = workListDef.VisitedItems;
			_issueStatePath = workListDef.Path;
		}

		public override IssueItem ParseRow(Row currentRow)
		{
			return new IssueItem((int)currentRow.GetObjectID(), currentRow, _attributeReader);
		}

		public override Row CreateRow(IssueItem item)
		{
			return null;
		}

	}


}

