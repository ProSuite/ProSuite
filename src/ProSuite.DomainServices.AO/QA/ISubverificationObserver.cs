using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Progress;

namespace ProSuite.DomainServices.AO.QA
{
	public interface ISubVerificationObserver : IDisposable
	{
		void CreatedSubverification(int idSubverification, [CanBeNull] EnvelopeXY area);

		void Started(int id, string workerAddress);

		void Finished(int id, ServiceCallStatus failed);
	}
}
