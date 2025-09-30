using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [ServiceContract(SessionMode = SessionMode.Required)]
    public interface IBatteryService
    {
        [OperationContract(IsInitiating = true, IsTerminating = false)]
        Response StartSession(EisMeta header);

        [OperationContract(IsInitiating = false, IsTerminating = false)]
        [FaultContract(typeof(ValidationFault))]
        [FaultContract(typeof(DataFormatFault))]
        Response PushSample(EisSample sample);

        [OperationContract(IsInitiating = false, IsTerminating = true)]
        Response EndSession();
    }
}
