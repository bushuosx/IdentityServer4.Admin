using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Skoruba.IdentityServer4.Admin.BusinessLogic.Services
{
    public interface IIdentityService : IBaseIdentityService<Guid, Guid, int>
    {
        //jj
        Task<int> ImportUserAsnyc(IEnumerable<string> userNames);
    }
}