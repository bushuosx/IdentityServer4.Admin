using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Skoruba.IdentityServer4.Admin.BusinessLogic.Repositories
{
	public interface IIdentityRepository : IBaseIdentityRepository<Guid, Guid, int>
    {
        Task<List<EntityFramework.Entities.Employee>> ImportUserAsnyc(List<EntityFramework.Entities.Employee> employees);
    }
}