using NOA.Common.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOA.Common.Service.Model
{
    public class InviteUserResult
    {
        public bool InviteSuccess { get; set; }
        public bool AddGroupSuccess { get; set; }
        public AddToGroupStatus AddToGroupStatus { get; set; }  
    }
}
