using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Skoruba.IdentityServer4.Admin.EntityFramework.Entities
{
    public class Employee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public string GH_工号 { get; set; }
        public string XM_姓名 { get; set; }
        public string SFZH_身份证号 { get; set; }
    }
}
