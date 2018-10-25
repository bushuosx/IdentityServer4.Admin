using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Skoruba.IdentityServer4.Admin.BusinessLogic.Services
{
    public interface IIdentityService : IBaseIdentityService<Guid, Guid, int>
    {
        //jj
        Task<List<EntityFramework.Entities.Employee>> ImportUserAsnyc(List<EntityFramework.Entities.Employee> userNames);
    }
}