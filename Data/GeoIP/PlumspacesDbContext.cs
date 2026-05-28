using Microsoft.EntityFrameworkCore;
using Host.Models.Email;

namespace Plumspaces.Data
{
    public class PlumspacesDbContext : DbContext
    {
        public PlumspacesDbContext(DbContextOptions<PlumspacesDbContext> options)
            : base(options)
        {
        }

        public DbSet<SlugRoute> SlugRoutes { get; set; }
        public DbSet<EmailRecipient> EmailRecipients => Set<EmailRecipient>();

    }

    public class SlugRoute
    {
        public int Id { get; set; }        // Primary key
        public string Slug { get; set; }   // The slug string
        public string  Area {get; set; } = "";
        public string Controller { get; set; }
        public string Action { get; set; }
    }
}
