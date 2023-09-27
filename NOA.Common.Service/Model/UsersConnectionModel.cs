using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOA.Common.Service.Model
{
    public class UsersConnectionModel
    {
        public string UsersServiceAppId { get; set; }
        public string NorskOffshoreAuthenticateServiceScope { get; set; }
        public string UsersBaseAddress { get; set; }
        public string AdminConsentRedirectApi { get; set; }
        public string AccessGroupId { get; set; }
    }
}
