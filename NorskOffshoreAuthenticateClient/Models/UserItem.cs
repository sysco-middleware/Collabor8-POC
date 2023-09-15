using Microsoft.Graph.Models;

namespace NorskOffshoreAuthenticateClient.Models
{
    public class UserItem
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string TenantId { get; set; }
        public string DisplayName { get; internal set; }
        public string GivenName { get; internal set; }
        public bool? Enabled { get; internal set; }
        public string MobilePhone { get; internal set; }
        public string Country { get; internal set; }
        public string StreetAddress { get; internal set; }
        public ProfilePhoto Photo { get; internal set; }
    }
}
