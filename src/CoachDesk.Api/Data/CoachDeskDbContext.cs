using CoachDesk.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoachDesk.Api.Data;

/// <summary>
/// Mapeo de consulta del esquema de Supabase. No se invoca Database.Migrate ni
/// EnsureCreated: el SQL de Supabase es la fuente de verdad del esquema.
/// </summary>
public sealed class CoachDeskDbContext(DbContextOptions<CoachDeskDbContext> options) : DbContext(options)
{
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Coach> Coaches => Set<Coach>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Measurement> Measurements => Set<Measurement>();
    public DbSet<Food> Foods => Set<Food>();
    public DbSet<NutritionPlan> NutritionPlans => Set<NutritionPlan>();
    public DbSet<MealEntry> Meals => Set<MealEntry>();
    public DbSet<AppointmentSlot> Appointments => Set<AppointmentSlot>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Profile>(entity =>
        {
            entity.ToTable("perfiles"); entity.HasKey(x => x.UserId);
            entity.Property(x => x.UserId).HasColumnName("usuario_id"); entity.Property(x => x.Role).HasColumnName("rol");
            entity.Property(x => x.IsActive).HasColumnName("activo"); entity.Property(x => x.CreatedAt).HasColumnName("creado_en");
        });
        builder.Entity<Coach>(entity =>
        {
            entity.ToTable("coaches"); entity.HasKey(x => x.CoachId); entity.Property(x => x.CoachId).HasColumnName("coach_id");
            entity.Property(x => x.UserId).HasColumnName("usuario_id"); entity.Property(x => x.FirstName).HasColumnName("nombre");
            entity.Property(x => x.LastName).HasColumnName("apellidos"); entity.Property(x => x.IsActive).HasColumnName("activo");
        });
        builder.Entity<Client>(entity =>
        {
            entity.ToTable("clientes"); entity.HasKey(x => x.ClientNumber); entity.Property(x => x.ClientNumber).HasColumnName("numero_cliente");
            entity.Property(x => x.UserId).HasColumnName("usuario_id"); entity.Property(x => x.FirstName).HasColumnName("nombre"); entity.Property(x => x.LastName).HasColumnName("apellidos");
            entity.Property(x => x.Sex).HasColumnName("sexo"); entity.Property(x => x.BirthDate).HasColumnName("fecha_nacimiento"); entity.Property(x => x.Phone).HasColumnName("telefono");
            entity.Property(x => x.Occupation).HasColumnName("ocupacion"); entity.Property(x => x.Goal).HasColumnName("objetivo"); entity.Property(x => x.CoachingStartDate).HasColumnName("fecha_inicio_asesoria");
            entity.Property(x => x.CreatedAt).HasColumnName("fecha_alta"); entity.Property(x => x.Status).HasColumnName("estado");
        });
        builder.Entity<Measurement>(entity =>
        {
            entity.ToTable("mediciones"); entity.HasKey(x => x.MeasurementId); entity.HasIndex(x => new { x.ClientNumber, x.Period, x.Month }).IsUnique();
            entity.Property(x => x.MeasurementId).HasColumnName("medicion_id"); entity.Property(x => x.ClientNumber).HasColumnName("numero_cliente"); entity.Property(x => x.Period).HasColumnName("periodo"); entity.Property(x => x.Month).HasColumnName("mes"); entity.Property(x => x.MeasurementDate).HasColumnName("fecha_medicion");
            entity.Property(x => x.Weight).HasColumnName("peso").HasPrecision(5, 2); entity.Property(x => x.Height).HasColumnName("estatura").HasPrecision(5, 2);
            entity.Property(x => x.Neck).HasColumnName("cuello").HasPrecision(5, 2); entity.Property(x => x.Chest).HasColumnName("pecho").HasPrecision(5, 2); entity.Property(x => x.Waist).HasColumnName("cintura").HasPrecision(5, 2); entity.Property(x => x.Hip).HasColumnName("cadera").HasPrecision(5, 2);
            entity.Property(x => x.Leg).HasColumnName("pierna").HasPrecision(5, 2); entity.Property(x => x.Biceps).HasColumnName("biceps").HasPrecision(5, 2); entity.Property(x => x.Calf).HasColumnName("pantorrilla").HasPrecision(5, 2);
            entity.Property(x => x.TricipitalSkinfold).HasColumnName("pliegue_tricipital").HasPrecision(5, 2); entity.Property(x => x.BicipitalSkinfold).HasColumnName("pliegue_bicipital").HasPrecision(5, 2);
            entity.Property(x => x.IliacCrestSkinfold).HasColumnName("pliegue_cresta_iliaca").HasPrecision(5, 2); entity.Property(x => x.ScapularSkinfold).HasColumnName("pliegue_escapular").HasPrecision(5, 2);
            entity.Property(x => x.BodyFatPercentage).HasColumnName("porcentaje_grasa").HasPrecision(5, 2); entity.Property(x => x.Confirmed).HasColumnName("confirmada"); entity.Property(x => x.ConfirmedAt).HasColumnName("fecha_confirmacion");
        });
        builder.Entity<Food>(entity => { entity.ToTable("alimentos"); entity.HasKey(x => x.FoodId); entity.Property(x => x.FoodId).HasColumnName("alimento_id"); entity.Property(x => x.Name).HasColumnName("nombre"); entity.Property(x => x.KcalPer100g).HasColumnName("kcal_por_100g"); entity.Property(x => x.ProteinPer100g).HasColumnName("proteina_por_100g"); entity.Property(x => x.CarbsPer100g).HasColumnName("carbos_por_100g"); entity.Property(x => x.FatPer100g).HasColumnName("grasas_por_100g"); entity.Property(x => x.IsActive).HasColumnName("activo"); });
        builder.Entity<NutritionPlan>(entity => { entity.ToTable("planes_nutricionales"); entity.HasKey(x => x.NutritionPlanId); entity.Property(x => x.NutritionPlanId).HasColumnName("plan_nut_id"); entity.Property(x => x.ClientNumber).HasColumnName("numero_cliente"); entity.Property(x => x.CoachId).HasColumnName("coach_id"); entity.Property(x => x.Goal).HasColumnName("objetivo"); entity.Property(x => x.KcalTarget).HasColumnName("kcal_meta"); entity.Property(x => x.ProteinTargetGrams).HasColumnName("proteina_meta_g"); entity.Property(x => x.CarbsTargetGrams).HasColumnName("carbos_meta_g"); entity.Property(x => x.FatTargetGrams).HasColumnName("grasas_meta_g"); entity.Property(x => x.StartDate).HasColumnName("fecha_inicio"); entity.Property(x => x.EndDate).HasColumnName("fecha_fin"); entity.Property(x => x.IsActive).HasColumnName("activo"); entity.Property(x => x.Version).HasColumnName("version"); });
        builder.Entity<MealEntry>(entity => { entity.ToTable("comidas"); entity.HasKey(x => x.MealId); entity.Property(x => x.MealId).HasColumnName("comida_id"); entity.Property(x => x.NutritionPlanId).HasColumnName("plan_nut_id"); entity.Property(x => x.Order).HasColumnName("orden"); entity.Property(x => x.Name).HasColumnName("nombre"); entity.Property(x => x.Time).HasColumnName("hora"); entity.Property(x => x.Description).HasColumnName("descripcion"); });
        builder.Entity<AppointmentSlot>(entity => { entity.ToTable("citas"); entity.HasKey(x => x.AppointmentId); entity.Property(x => x.AppointmentId).HasColumnName("cita_id"); entity.Property(x => x.CoachId).HasColumnName("coach_id"); entity.Property(x => x.ClientNumber).HasColumnName("numero_cliente"); entity.Property(x => x.Date).HasColumnName("fecha"); entity.Property(x => x.StartsAt).HasColumnName("hora_inicio"); entity.Property(x => x.EndsAt).HasColumnName("hora_fin"); entity.Property(x => x.Status).HasColumnName("estado"); entity.Property(x => x.Modality).HasColumnName("modalidad"); entity.Property(x => x.Detail).HasColumnName("detalle"); entity.Property(x => x.ReservedAt).HasColumnName("reservada_en"); entity.Property(x => x.Version).HasColumnName("version"); });
    }
}
