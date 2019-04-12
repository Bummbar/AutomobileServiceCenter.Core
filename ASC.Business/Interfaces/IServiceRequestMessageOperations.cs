using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ASC.Models.Models;

namespace ASC.Business.Interfaces
{
    public interface IServiceRequestMessageOperations
    {
        Task CreateServiceRequestMessageAsync(ServiceRequestMessage message);
        Task<List<ServiceRequestMessage>> GetServiceRequestMessageAsync(string serviceRequestId);
    }
}
