using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOA.Common.Service.Model
{
    public class AzureAdOptions
    {
        public string Instance { get; set; }
        public string Domain { get; set; } 
        public string TenantId { get; set; } 
        public string ClientId { get; set; } 
        public string ClientSecret { get; set; } 
        public List<string> ClientCapabilities { get; set; } 
        public List<string>  AllowedTenants { get; set; }
    }
}
