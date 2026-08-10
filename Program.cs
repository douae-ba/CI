using CI.Data;
using CI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<CI.Services.SuggestionImportService>();
builder.Services.AddScoped<CI.Services.TargetExportService>();
builder.Services.AddScoped<CI.Services.TargetStatsService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    // Récupérer la base de données AVANT de l'utiliser
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Appliquer les migrations
    db.Database.Migrate();

    // Supprimer les anciens scopes et transférer leurs suggestions vers "Other"
    var scopesToRemove = new[] { "Innovator", "Laboratory" };

    var otherScope = db.Scopes.FirstOrDefault(s => s.Nom == "Other");

    if (otherScope != null)
    {
        foreach (var name in scopesToRemove)
        {
            var scopeToDelete = db.Scopes.FirstOrDefault(s => s.Nom == name);

            if (scopeToDelete != null)
            {
                var affectedSuggestions = db.Suggestions
                    .Where(s => s.ScopeId == scopeToDelete.Id)
                    .ToList();

                foreach (var s in affectedSuggestions)
                {
                    s.ScopeId = otherScope.Id;
                }

                var affectedObjectives = db.DepartementScopeObjectives
                    .Where(o => o.ScopeId == scopeToDelete.Id)
                    .ToList();

                db.DepartementScopeObjectives.RemoveRange(affectedObjectives);

                db.Scopes.Remove(scopeToDelete);
            }
        }

        db.SaveChanges();
    }

    // Créer l'utilisateur admin s'il n'existe aucun utilisateur
    if (!db.Users.Any())
    {
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();

        var user = new User
        {
            Username = "adminCI"
        };

        user.PasswordHash = hasher.HashPassword(user, "admin");

        db.Users.Add(user);
        db.SaveChanges();
    }

    // Seed des départements
    var departementsToSeed = new[]
    {
        "Eng Method",
        "Eng Process",
        "Finance",
        "HR",
        "Logistic",
        "Maintenance",
        "Production",
        "QHSE"
    };

    foreach (var name in departementsToSeed)
    {
        bool exists = db.Departements
            .Any(d => d.Nom.ToLower() == name.ToLower());

        if (!exists)
        {
            db.Departements.Add(new Departement
            {
                Nom = name
            });
        }
    }

    // Seed des scopes
    var scopesToSeed = new[]
    {
        "5S",
        "Cost Reduction",
        "Environment",
        "Finance",
        "Health and Safety",
        "Innovator",
        "Laboratory",
        "Other",
        "Productivity",
        "Quality",
        "Scrap",
        "WPO"
    };

    foreach (var name in scopesToSeed)
    {
        bool exists = db.Scopes
            .Any(s => s.Nom.ToLower() == name.ToLower());

        if (!exists)
        {
            db.Scopes.Add(new Scope
            {
                Nom = name
            });
        }
    }

    // Sauvegarder les données ajoutées
    db.SaveChanges();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    // The default HSTS value is 30 days.
    // You may want to change this for production scenarios.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();