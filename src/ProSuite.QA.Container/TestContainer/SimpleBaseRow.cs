using System;
using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.QA.Container.TestContainer
{
	public class SimpleBaseRow : BaseRow
	{
		public SimpleBaseRow([NotNull] IReadOnlyFeature feature)
			: base(feature, ProxyUtils.CreateBox(feature.Shape), GetOidList(feature)) { }

		protected override Box GetExtent()
		{
			throw new InvalidOperationException("Box was transferred with Constructor");
		}

		protected override IList<long> GetOidList()
		{
			throw new NotImplementedException("Box was transferred with Constructor");
		}
	}
}
