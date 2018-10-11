using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Skoruba.IdentityServer4.Admin.BusinessLogic.Repositories
{
	public interface IIdentityRepository : IBaseIdentityRepository<Guid, Guid, int>
    {
        Task<int> ImportUserAsnyc(IEnumerable<string> userNames);
    }
}