using Microsoft.Graph.Models;

namespace NOA.Common.Service.Model
{
    public class UserItem
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string TenantId { get; set; }
        public string DisplayName { get; set; }
        public string GivenName { get; set; }
        public bool? Enabled { get; set; }
        public string MobilePhone { get; set; }
        public string Country { get; set; }
        public string StreetAddress { get; set; }
        public ProfilePhoto Photo { get; set; }
    }
}
