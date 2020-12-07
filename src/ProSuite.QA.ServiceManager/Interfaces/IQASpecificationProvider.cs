using System.Collections.Generic;

namespace ProSuite.QA.ServiceManager.Interfaces
{
    // QA specification can provide DDX, XML, ....
    public interface IQASpecificationProvider
    {
        IList<string> GetQASpecificationNames();

        string GetQASpecificationsConnection(string name);
    }
}
