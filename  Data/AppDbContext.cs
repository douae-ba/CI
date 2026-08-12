using Microsoft.EntityFrameworkCore;
using CI.Models;

namespace CI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Suggestion> Suggestions { get; set; }
        public DbSet<Departement> Departements { get; set; }
        public DbSet<Scope> Scopes { get; set; }
        public DbSet<Exports> Exports { get; set; }
        public DbSet<StatutObjective> StatutObjectives { get; set; }
        public DbSet<DepartementAnnualObjective> DepartementAnnualObjectives { get; set; }
        public DbSet<DepartementMonthlyObjective> DepartementMonthlyObjectives { get; set; }
        public DbSet<DepartementScopeObjective> DepartementScopeObjectives { get; set; }
        public DbSet<TargetRecord> TargetRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Suggestion>()
                .HasIndex(s => s.NumeroSuggestion); 

            modelBuilder.Entity<Suggestion>()
                .HasIndex(s => s.SourceRowNo)
                .IsUnique();

            modelBuilder.Entity<Departement>()
                .HasIndex(d => d.Nom)
                .IsUnique();

            modelBuilder.Entity<Scope>()
                .HasIndex(s => s.Nom)
                .IsUnique();

            modelBuilder.Entity<StatutObjective>().HasIndex(s => new { s.Statut, s.Year, s.Mois }).IsUnique();
            modelBuilder.Entity<DepartementAnnualObjective>().HasIndex(a => new { a.DepartementId, a.Year }).IsUnique();
            modelBuilder.Entity<DepartementMonthlyObjective>().HasIndex(m => new { m.DepartementId, m.Year, m.Mois }).IsUnique();

            modelBuilder.Entity<DepartementScopeObjective>()
                .HasIndex(o => new { o.DepartementId, o.ScopeId, o.Year, o.Mois })
                .IsUnique();

            modelBuilder.Entity<TargetRecord>()
                .HasIndex(t => new { t.SourceFilePath, t.SuggestionNumber });

            base.OnModelCreating(modelBuilder);
        }
    }
}