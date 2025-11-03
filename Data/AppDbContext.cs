using HSB.BE.Models;
using Microsoft.EntityFrameworkCore;

namespace HSB.BE.Data
{
	public class AppDbContext : DbContext
	{
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
		{
		}

		public DbSet<User> Users { get; set; }
	}
}