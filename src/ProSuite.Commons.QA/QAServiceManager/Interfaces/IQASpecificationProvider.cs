using System.Collections.Generic;

namespace ProSuite.Commons.QA.ServiceManager.Interfaces
{
    // QA specification can provide DDX, XML, ....
    public interface IQASpecificationProvider
    {
        IList<string> GetQASpecificationNames();
    }
}
