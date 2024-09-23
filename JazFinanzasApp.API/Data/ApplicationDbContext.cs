using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {

        }
    }
}
