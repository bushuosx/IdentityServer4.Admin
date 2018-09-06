using Skoruba.IdentityServer4.Admin.BusinessLogic.Dtos.Identity.Base;
using System;

namespace Skoruba.IdentityServer4.Admin.BusinessLogic.Dtos.Identity
{
    public class UserProviderDto : BaseUserProviderDto<Guid>
    {
        public string ProviderKey { get; set; }

        public string LoginProvider { get; set; }

        public string ProviderDisplayName { get; set; }        
    }
}
