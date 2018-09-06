using System;
using System.ComponentModel.DataAnnotations;
using Skoruba.IdentityServer4.Admin.BusinessLogic.Dtos.Identity.Base;

namespace Skoruba.IdentityServer4.Admin.BusinessLogic.Dtos.Identity
{
    public class RoleDto : BaseRoleDto<Guid>
    {      
        [Required]
        public string Name { get; set; }
    }
}