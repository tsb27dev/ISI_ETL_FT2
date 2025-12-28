using Microsoft.EntityFrameworkCore; // This is the crucial missing reference
using SmartGardenApi.Models;

namespace SmartGardenApi.Data;

// Ensure it inherits from DbContext
public class GardenContext : DbContext
{
    public GardenContext(DbContextOptions<GardenContext> options) : base(options) { }

    public DbSet<Plant> Plants { get; set; }
    public DbSet<User> Users { get; set; } // <--- ADICIONA ISTO
}