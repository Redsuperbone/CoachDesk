using CoachDesk.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoachDesk.Api.Data;

public sealed class CoachDeskDbContext(DbContextOptions<CoachDeskDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Advisee> Advisees => Set<Advisee>();
    public DbSet<AnthropometricAssessment> AnthropometricAssessments => Set<AnthropometricAssessment>();
    public DbSet<MealPlan> MealPlans => Set<MealPlan>();
    public DbSet<Meal> Meals => Set<Meal>();
    public DbSet<NutritionItem> NutritionItems => Set<NutritionItem>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<User>().HasIndex(user => user.Email).IsUnique();
        builder.Entity<Advisee>().HasIndex(advisee => advisee.Email);
        builder.Entity<AnthropometricAssessment>().HasIndex(assessment => new { assessment.AdviseeId, assessment.MonthNumber }).IsUnique();
        builder.Entity<Appointment>().HasIndex(appointment => new { appointment.CoachId, appointment.StartsAt });
        builder.Entity<MealPlan>().HasOne(plan => plan.Advisee).WithMany(advisee => advisee.MealPlans).HasForeignKey(plan => plan.AdviseeId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Meal>().HasOne(meal => meal.MealPlan).WithMany(plan => plan.Meals).HasForeignKey(meal => meal.MealPlanId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<NutritionItem>().HasOne(item => item.Meal).WithMany(meal => meal.Items).HasForeignKey(item => item.MealId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Appointment>().HasOne(appointment => appointment.Advisee).WithMany().HasForeignKey(appointment => appointment.AdviseeId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<AnthropometricAssessment>().Property(value => value.WeightKg).HasPrecision(6, 2);
        builder.Entity<NutritionItem>().Property(value => value.Calories).HasPrecision(8, 2);
    }
}
