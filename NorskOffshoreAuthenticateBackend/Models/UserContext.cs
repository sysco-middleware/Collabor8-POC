using Microsoft.EntityFrameworkCore;

namespace NorskOffshoreAuthenticateBackend.Models
{
    public class UserContext : DbContext
    {
        public UserContext(DbContextOptions<UserContext> options)
            : base(options)
        {

        }

        public DbSet<UserItem> UserItems { get; set; }
    }
}
