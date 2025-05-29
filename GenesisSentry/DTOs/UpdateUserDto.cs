using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisSentry.DTOs
{
    public class UpdateUserDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; }

        public string Role {  get; set; }
        public bool? IsActive { get; set; }

        public string Password { get; set; }
    }
}
