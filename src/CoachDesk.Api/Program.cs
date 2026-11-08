using System.Security.Claims;
using System.Text;
using CoachDesk.Api.Contracts;
using CoachDesk.Api.Data;
using CoachDesk.Api.Domain;
using CoachDesk.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("CoachDesk") ?? throw new InvalidOperationException("Connection string CoachDesk is required.");
var mysqlServerVersion = builder.Configuration["MySql:ServerVersion"] ?? "8.0.36-mysql";
builder.Services.AddDbContext<CoachDeskDbContext>(options => options.UseMySql(connectionString, ServerVersion.Parse(mysqlServerVersion)));
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var key = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is required.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true, ValidIssuer = builder.Configuration["Jwt:Issuer"], ValidateAudience = true, ValidAudience = builder.Configuration["Jwt:Audience"],
    ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), ValidateLifetime = true
});
builder.Services.AddAuthorization();

var app = builder.Build();
await DatabaseInitializer.InitializeAsync(app.Services, app.Configuration, app.Logger);
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

var api = app.MapGroup("/api");
api.MapPost("/auth/login", async (LoginRequest request, CoachDeskDbContext db, PasswordService passwords, JwtService jwt) =>
{
    var user = await db.Users.SingleOrDefaultAsync(item => item.Email == request.Email.Trim().ToLowerInvariant() && item.IsActive);
    if (user is null || !passwords.Verify(request.Password, user.PasswordHash)) return Results.Unauthorized();
    if (user.Role != UserRole.Coach) return Results.Forbid();
    var (token, expiresAt) = jwt.Create(user);
    return Results.Ok(new LoginResponse(token, "Bearer", expiresAt, new CoachResponse(user.Id, user.FullName, user.Email, user.Role.ToString())));
});

var coach = api.MapGroup("/coach").RequireAuthorization(policy => policy.RequireRole(UserRole.Coach.ToString()));
coach.MapGet("/advisees", async (bool? active, string? search, CoachDeskDbContext db) =>
{
    var query = db.Advisees.AsNoTracking().AsQueryable();
    if (active.HasValue) query = query.Where(item => item.IsActive == active.Value);
    if (!string.IsNullOrWhiteSpace(search)) query = query.Where(item => item.FullName.Contains(search.Trim()));
    return await query.OrderBy(item => item.FullName).Select(item => new AdviseeSummaryResponse(item.Id, item.FullName, item.Email, item.PhoneNumber, item.BirthDate, item.IsActive, item.LastAssessmentAt)).ToListAsync();
});

coach.MapGet("/advisees/{adviseeId:guid}/anthropometry", async (Guid adviseeId, CoachDeskDbContext db) =>
{
    var storedAssessments = await db.AnthropometricAssessments
        .AsNoTracking()
        .Where(item => item.AdviseeId == adviseeId)
        .OrderBy(item => item.MonthNumber)
        .ToListAsync();
    var assessments = storedAssessments.Select(ToAssessment).ToList();
    return Results.Ok(new AnthropometricProfileResponse(adviseeId, assessments));
});
coach.MapPut("/advisees/{adviseeId:guid}/anthropometry", async (Guid adviseeId, AnthropometricProfileRequest request, CoachDeskDbContext db) =>
{
    if (!await db.Advisees.AnyAsync(item => item.Id == adviseeId)) return Results.NotFound();
    if (request.Assessments.Any(item => item.MonthNumber is < 1 or > 10)) return Results.BadRequest(new { message = "El mes debe estar entre 1 y 10." });
    var existing = await db.AnthropometricAssessments.Where(item => item.AdviseeId == adviseeId).ToListAsync();
    db.AnthropometricAssessments.RemoveRange(existing);
    db.AnthropometricAssessments.AddRange(request.Assessments.Select(item => FromAssessment(adviseeId, item)));
    await db.SaveChangesAsync();
    return Results.NoContent();
});

coach.MapGet("/advisees/{adviseeId:guid}/nutrition-plan", async (Guid adviseeId, CoachDeskDbContext db) =>
{
    var plan = await db.MealPlans.Include(item => item.Meals).ThenInclude(item => item.Items).OrderByDescending(item => item.EffectiveDate).FirstOrDefaultAsync(item => item.AdviseeId == adviseeId);
    return plan is null ? Results.NotFound() : Results.Ok(ToMealPlan(plan));
});
coach.MapPut("/advisees/{adviseeId:guid}/nutrition-plan", async (Guid adviseeId, MealPlanRequest request, CoachDeskDbContext db) =>
{
    if (!await db.Advisees.AnyAsync(item => item.Id == adviseeId)) return Results.NotFound();
    var existing = await db.MealPlans.Include(item => item.Meals).ThenInclude(item => item.Items).OrderByDescending(item => item.EffectiveDate).FirstOrDefaultAsync(item => item.AdviseeId == adviseeId);
    if (existing is not null) db.MealPlans.Remove(existing);
    var plan = new MealPlan { AdviseeId = adviseeId, Name = request.Name.Trim(), EffectiveDate = request.EffectiveDate, Notes = request.Notes, Meals = request.Meals.Select(meal => new Meal { Type = meal.Type, Items = meal.Items.Select(item => new NutritionItem { FoodName = item.FoodName, Portion = item.Portion, Calories = item.Calories, ProteinGrams = item.ProteinGrams, CarbohydratesGrams = item.CarbohydratesGrams, FatGrams = item.FatGrams }).ToList() }).ToList() };
    db.MealPlans.Add(plan); await db.SaveChangesAsync(); return Results.NoContent();
});

coach.MapGet("/appointments", async (DateOnly from, DateOnly to, ClaimsPrincipal principal, CoachDeskDbContext db) =>
{
    if (to < from) return Results.BadRequest(new { message = "El rango de fechas no es válido." });

    var coachId = GetUserId(principal);
    var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    var end = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    var storedAppointments = await db.Appointments
        .AsNoTracking()
        .Include(item => item.Advisee)
        .Where(item => item.CoachId == coachId && item.StartsAt >= start && item.StartsAt < end)
        .OrderBy(item => item.StartsAt)
        .ToListAsync();

    return Results.Ok(storedAppointments.Select(ToAppointment).ToList());
});
coach.MapPost("/appointments", async (CreateAvailabilityRequest request, ClaimsPrincipal principal, CoachDeskDbContext db) =>
{
    if (request.DurationMinutes is < 15 or > 240 || string.IsNullOrWhiteSpace(request.Detail)) return Results.BadRequest(new { message = "Datos de horario inválidos." });
    var coachId = GetUserId(principal);
    var end = request.StartsAt.AddMinutes(request.DurationMinutes);
    var overlap = await db.Appointments.AnyAsync(item => item.CoachId == coachId && item.Status != AppointmentStatus.Cancelled && request.StartsAt < item.StartsAt.AddMinutes(item.DurationMinutes) && end > item.StartsAt);
    if (overlap) return Results.Conflict(new { message = "El horario se traslapa con una cita publicada." });
    var appointment = new Appointment { CoachId = coachId, StartsAt = request.StartsAt, DurationMinutes = request.DurationMinutes, Detail = request.Detail.Trim() };
    db.Appointments.Add(appointment); await db.SaveChangesAsync(); return Results.Created($"/api/coach/appointments/{appointment.Id}", ToAppointment(appointment));
});
coach.MapPost("/appointments/{appointmentId:guid}/cancel", async (Guid appointmentId, ClaimsPrincipal principal, CoachDeskDbContext db) =>
{
    var coachId = GetUserId(principal);
    var item = await db.Appointments.SingleOrDefaultAsync(item => item.Id == appointmentId && item.CoachId == coachId);
    if (item is null) return Results.NotFound();
    item.Status = AppointmentStatus.Cancelled;
    await db.SaveChangesAsync();
    return Results.NoContent();
});
coach.MapDelete("/appointments/{appointmentId:guid}", async (Guid appointmentId, ClaimsPrincipal principal, CoachDeskDbContext db) =>
{
    var coachId = GetUserId(principal);
    var item = await db.Appointments.SingleOrDefaultAsync(item => item.Id == appointmentId && item.CoachId == coachId);
    if (item is null) return Results.NotFound();
    if (item.Status == AppointmentStatus.Booked) return Results.Conflict(new { message = "Cancela una cita ocupada antes de eliminarla." });
    db.Appointments.Remove(item);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

static Guid GetUserId(ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub") ?? throw new UnauthorizedAccessException());
static AnthropometricAssessmentRequest ToAssessment(AnthropometricAssessment item) => new(item.MonthNumber, item.RecordedOn, item.WeightKg, item.HeightCm, item.BicipitalSkinfoldMm, item.TricipitalSkinfoldMm, item.SubscapularSkinfoldMm, item.SuprailiacSkinfoldMm, item.WaistCm, item.HipCm, item.ArmCm, item.ThighCm);
static AnthropometricAssessment FromAssessment(Guid adviseeId, AnthropometricAssessmentRequest item) => new() { AdviseeId = adviseeId, MonthNumber = item.MonthNumber, RecordedOn = item.RecordedOn, WeightKg = item.WeightKg, HeightCm = item.HeightCm, BicipitalSkinfoldMm = item.BicipitalSkinfoldMm, TricipitalSkinfoldMm = item.TricipitalSkinfoldMm, SubscapularSkinfoldMm = item.SubscapularSkinfoldMm, SuprailiacSkinfoldMm = item.SuprailiacSkinfoldMm, WaistCm = item.WaistCm, HipCm = item.HipCm, ArmCm = item.ArmCm, ThighCm = item.ThighCm };
static MealPlanResponse ToMealPlan(MealPlan plan) => new(plan.Id, plan.AdviseeId, plan.Name, plan.EffectiveDate, plan.Notes, plan.Meals.Select(meal => new MealRequest(meal.Type, meal.Items.Select(item => new NutritionItemRequest(item.FoodName, item.Portion, item.Calories, item.ProteinGrams, item.CarbohydratesGrams, item.FatGrams)).ToList())).ToList());
static AppointmentResponse ToAppointment(Appointment appointment) => new(appointment.Id, appointment.StartsAt, appointment.DurationMinutes, appointment.Detail, appointment.Advisee?.FullName, appointment.Status);
