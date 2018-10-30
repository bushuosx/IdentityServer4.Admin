using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Skoruba.IdentityServer4.AspNetIdentity.Quickstart.Account
{
    public class ResetPasswordViewModel
    {
        [Required]
        [DisplayName("工号")]
        public string UserName { get; set; }

        //[Required]
        [DataType(DataType.Password)]
        [DisplayName("身份证后4位")]
        public string OldPassword { get; set; }

        [EmailAddress]
        [DisplayName("Email")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [DisplayName("密码")]
        public string Password { get; set; }

        [Compare(nameof(Password))]
        [DataType(DataType.Password)]
        [DisplayName("确认密码")]
        public string ConfirmPassword { get; set; }

        public string ReturnUrl { get; set; }
    }
}
