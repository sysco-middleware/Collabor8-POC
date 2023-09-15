using Microsoft.EntityFrameworkCore;

namespace NorskOffshoreAuthenticateService.Models
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
