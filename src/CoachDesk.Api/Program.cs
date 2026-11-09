using System.Globalization;
using System.Security.Claims;
using System.Text.Json.Nodes;
using CoachDesk.Api.Configuration;
using CoachDesk.Api.Contracts;
using CoachDesk.Api.Data;
using CoachDesk.Api.Domain;
using CoachDesk.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment()) builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

var connectionString = builder.Configuration.GetConnectionString("CoachDesk") ?? throw new InvalidOperationException("Connection string CoachDesk is required.");
builder.Services.AddDbContext<CoachDeskDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddOptions<SupabaseOptions>().BindConfiguration(SupabaseOptions.SectionName)
    .Validate(value => Uri.TryCreate(value.Url, UriKind.Absolute, out _), "Supabase:Url debe ser una URL absoluta.")
    .Validate(value => !string.IsNullOrWhiteSpace(value.PublishableKey), "Supabase:PublishableKey es obligatorio.").ValidateOnStart();
builder.Services.AddHttpClient<SupabaseClient>();
builder.Services.AddAuthentication("Supabase").AddScheme<AuthenticationSchemeOptions, SupabaseAuthenticationHandler>("Supabase", _ => { });
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
await DatabaseInitializer.VerifyConnectionAsync(app.Services, app.Logger);
app.UseExceptionHandler(errors => errors.Run(async context =>
{
    var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    var status = exception is SupabaseRequestException supabaseError ? (int)supabaseError.StatusCode : StatusCodes.Status500InternalServerError;
    context.Response.StatusCode = status;
    await context.Response.WriteAsJsonAsync(new { message = status >= 500 ? "No fue posible completar la operación." : exception?.Message });
}));
app.UseSwagger(); app.UseSwaggerUI(); app.UseHttpsRedirection(); app.UseAuthentication(); app.UseAuthorization();

var api = app.MapGroup("/api");
api.MapPost("/auth/login", async (LoginRequest request, SupabaseClient supabase, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password)) return Results.BadRequest(new { message = "Correo y contraseña son obligatorios." });
    var session = await supabase.SignInAsync(request.Email.Trim(), request.Password, ct);
    if (session is null) return Results.Unauthorized();
    var coach = await GetCoachAsync(session.UserId, session.AccessToken, supabase, ct);
    if (coach is null) return Results.Forbid();
    return Results.Ok(new LoginResponse(session.AccessToken, "Bearer", DateTimeOffset.UtcNow.AddSeconds(session.ExpiresIn ?? 3600), new CoachResponse(coach.Id, coach.Name, session.Email, coach.Role)));
});

var coachApi = api.MapGroup("/coach").RequireAuthorization();
coachApi.MapGet("/advisees", async (bool? active, string? search, HttpRequest request, ClaimsPrincipal principal, SupabaseClient supabase, CancellationToken ct) =>
{
    var token = Token(request); if (await GetCoachAsync(UserId(principal), token, supabase, ct) is null) return Results.Forbid();
    var query = "clientes?select=numero_cliente,nombre,apellidos,telefono,fecha_nacimiento,estado,fecha_alta&order=apellidos.asc,nombre.asc";
    if (active == true) query += "&estado=eq.Activo";
    else if (active == false) query += "&or=(estado.eq.Suspendido,estado.eq.Baja)";
    if (!string.IsNullOrWhiteSpace(search)) { var term = Uri.EscapeDataString("*" + search.Trim() + "*"); query += "&or=(nombre.ilike." + term + ",apellidos.ilike." + term + ")"; }
    var rows = await supabase.GetRowsAsync(query, token, ct);
    return Results.Ok(rows.OfType<JsonObject>().Select(row => new AdviseeSummaryResponse(Int(row, "numero_cliente"), Name(row), null, Text(row, "telefono"), Date(row, "fecha_nacimiento"), string.Equals(Text(row, "estado"), "Activo", StringComparison.OrdinalIgnoreCase), Text(row, "estado") ?? string.Empty, Timestamp(row, "fecha_alta"))).ToList());
});
coachApi.MapPost("/advisees", async (CreateAdviseeRequest body, HttpRequest request, ClaimsPrincipal principal, SupabaseClient supabase, CancellationToken ct) =>
{
    var token = Token(request); if (await GetCoachAsync(UserId(principal), token, supabase, ct) is null) return Results.Forbid();
    if (string.IsNullOrWhiteSpace(body.FirstName) || body.FirstName.Trim().Length < 2 || string.IsNullOrWhiteSpace(body.LastName)) return Results.BadRequest(new { message = "Nombre y apellidos son obligatorios." });
    if (body.Sex is not null && body.Sex is not ("H" or "M")) return Results.BadRequest(new { message = "El sexo debe ser H, M o quedar vacío." });
    var inserted = await supabase.PostRowsAsync("clientes?select=numero_cliente,nombre,apellidos,telefono,fecha_nacimiento,estado,fecha_alta", new JsonObject
    {
        ["nombre"] = body.FirstName.Trim(), ["apellidos"] = body.LastName.Trim(), ["sexo"] = body.Sex,
        ["fecha_nacimiento"] = body.BirthDate?.ToString("yyyy-MM-dd"), ["telefono"] = Clean(body.PhoneNumber), ["ocupacion"] = Clean(body.Occupation),
        ["objetivo"] = Clean(body.Goal), ["fecha_inicio_asesoria"] = (body.CoachingStartDate ?? DateOnly.FromDateTime(DateTime.Today)).ToString("yyyy-MM-dd"), ["estado"] = "Activo"
    }, token, "return=representation", ct);
    if (inserted.FirstOrDefault() is not JsonObject row) throw new InvalidOperationException("Supabase no devolvió el cliente creado.");
    var response = new AdviseeSummaryResponse(Int(row, "numero_cliente"), Name(row), null, Text(row, "telefono"), Date(row, "fecha_nacimiento"), true, Text(row, "estado") ?? "Activo", Timestamp(row, "fecha_alta"));
    return Results.Created($"/api/coach/advisees/{response.Id}", response);
});

coachApi.MapPut("/advisees/{id:int}/service-status", async (int id, UpdateAdviseeServiceStatusRequest body, HttpRequest request, ClaimsPrincipal principal, SupabaseClient supabase, CancellationToken ct) =>
{
    var token = Token(request); if (await GetCoachAsync(UserId(principal), token, supabase, ct) is null) return Results.Forbid();
    await supabase.PatchAsync($"clientes?numero_cliente=eq.{id}", new JsonObject { ["estado"] = body.IsActive ? "Activo" : "Suspendido" }, token, ct); return Results.NoContent();
});
// La baja es lógica: el esquema no define eliminación física de clientes para coaches.
coachApi.MapDelete("/advisees/{id:int}", async (int id, HttpRequest request, ClaimsPrincipal principal, SupabaseClient supabase, CancellationToken ct) =>
{
    var token = Token(request); if (await GetCoachAsync(UserId(principal), token, supabase, ct) is null) return Results.Forbid();
    await supabase.PatchAsync($"clientes?numero_cliente=eq.{id}", new JsonObject { ["estado"] = "Baja" }, token, ct); return Results.NoContent();
});

coachApi.MapGet("/advisees/{id:int}/anthropometry", async (int id, HttpRequest request, ClaimsPrincipal principal, SupabaseClient supabase, CancellationToken ct) =>
{
    var token = Token(request); if (await GetCoachAsync(UserId(principal), token, supabase, ct) is null) return Results.Forbid();
    var rows = await supabase.GetRowsAsync($"mediciones?numero_cliente=eq.{id}&select=mes,fecha_medicion,peso,estatura,pliegue_bicipital,pliegue_tricipital,pliegue_escapular,pliegue_cresta_iliaca,cintura,cadera,biceps,pierna&order=mes.asc", token, ct);
    return Results.Ok(new AnthropometricProfileResponse(id, rows.OfType<JsonObject>().Select(ToAssessment).ToList()));
});
coachApi.MapPut("/advisees/{id:int}/anthropometry", async (int id, AnthropometricProfileRequest body, HttpRequest request, ClaimsPrincipal principal, SupabaseClient supabase, CancellationToken ct) =>
{
    var token = Token(request); if (await GetCoachAsync(UserId(principal), token, supabase, ct) is null) return Results.Forbid();
    if (body.Assessments.Any(item => item.MonthNumber is < 0 or > 10)) return Results.BadRequest(new { message = "El mes debe estar entre Inicio (0) y 10." });
    var rows = new JsonArray(body.Assessments.Select(item => Measurement(id, item)).ToArray());
    if (rows.Count > 0) await supabase.PostRowsAsync("mediciones?on_conflict=numero_cliente,mes", rows, token, "resolution=merge-duplicates,return=minimal", ct);
    return Results.NoContent();
});
coachApi.MapGet("/advisees/{id:int}/photos", async (int id, HttpRequest request, ClaimsPrincipal principal, SupabaseClient supabase, CancellationToken ct) =>
{
    var token = Token(request); if (await GetCoachAsync(UserId(principal), token, supabase, ct) is null) return Results.Forbid();
    var rows = await supabase.GetRowsAsync($"fotos_control?numero_cliente=eq.{id}&select=foto_id,mes,tipo,ruta_archivo,tamano_bytes,fecha_subida&order=mes.asc,tipo.asc", token, ct);
    return Results.Ok(rows.OfType<JsonObject>().Select(row => new PhotoEvidenceResponse(Int(row, "foto_id"), Int(row, "mes"), Text(row, "tipo") ?? string.Empty, Path.GetFileName(Text(row, "ruta_archivo") ?? string.Empty), row["tamano_bytes"]?.GetValue<long>() ?? 0, Timestamp(row, "fecha_subida") ?? DateTimeOffset.MinValue)).ToList());
});
coachApi.MapGet("/advisees/{id:int}/photos/{photoId:int}/content", async (int id, int photoId, HttpRequest request, ClaimsPrincipal principal, SupabaseClient supabase, CancellationToken ct) =>
{
    var token = Token(request); if (await GetCoachAsync(UserId(principal), token, supabase, ct) is null) return Results.Forbid();
    var rows = await supabase.GetRowsAsync($"fotos_control?foto_id=eq.{photoId}&numero_cliente=eq.{id}&select=ruta_archivo", token, ct);
    if (rows.FirstOrDefault() is not JsonObject photo || string.IsNullOrWhiteSpace(Text(photo, "ruta_archivo"))) return Results.NotFound();
    var file = await supabase.DownloadStorageObjectAsync("fotos-control", Text(photo, "ruta_archivo")!, token, ct);
    return Results.File(file.Content, file.ContentType);
});
coachApi.MapGet("/advisees/{id:int}/training-plans", async (int id, HttpRequest request, ClaimsPrincipal principal, SupabaseClient supabase, CancellationToken ct) =>
{
    var token = Token(request); if (await GetCoachAsync(UserId(principal), token, supabase, ct) is null) return Results.Forbid();
    var rows = await supabase.GetRowsAsync($"planes_entrenamiento?numero_cliente=eq.{id}&select=plan_id,nombre,fecha_inicio,fecha_fin,activo,version&order=activo.desc,fecha_inicio.desc,plan_id.desc", token, ct);
    return Results.Ok(rows.OfType<JsonObject>().Select(row => new TrainingPlanSummaryResponse(Int(row, "plan_id"), Text(row, "nombre") ?? string.Empty, Date(row, "fecha_inicio") ?? DateOnly.MinValue, Date(row, "fecha_fin"), Bool(row, "activo"), Int(row, "version"))).ToList());
});
coachApi.MapGet("/advisees/{id:int}/training-plans/{planId:int}", async (int id, int planId, HttpRequest request, ClaimsPrincipal principal, SupabaseClient supabase, CancellationToken ct) =>
{
    var token = Token(request); if (await GetCoachAsync(UserId(principal), token, supabase, ct) is null) return Results.Forbid();
    var plans = await supabase.GetRowsAsync($"planes_entrenamiento?plan_id=eq.{planId}&numero_cliente=eq.{id}&select=plan_id,nombre,fecha_inicio,version", token, ct);
    if (plans.FirstOrDefault() is not JsonObject plan) return Results.NotFound(new { message = "La rutina seleccionada no existe para este asesorado." });
    var dayRows = await supabase.GetRowsAsync($"plan_dias?plan_id=eq.{planId}&select=plan_dia_id,dia_semana,titulo&order=dia_semana.asc", token, ct);
    var days = new List<TrainingPlanDayResponse>();
    foreach (var day in dayRows.OfType<JsonObject>())
    {
        var exercises = await supabase.GetRowsAsync($"plan_dia_ejercicios?plan_dia_id=eq.{Int(day, "plan_dia_id")}&select=series,reps_min,reps_max,descanso_seg,carga_sugerida_kg,ejercicios(nombre)&order=orden.asc", token, ct);
        days.Add(new TrainingPlanDayResponse((short)Int(day, "dia_semana"), Text(day, "titulo") ?? string.Empty, exercises.OfType<JsonObject>().Select(exercise => new TrainingPlanExerciseResponse(exercise["ejercicios"] is JsonObject definition ? Text(definition, "nombre") ?? string.Empty : string.Empty, (short)Int(exercise, "series"), (short)Int(exercise, "reps_min"), (short)Int(exercise, "reps_max"), (short)Int(exercise, "descanso_seg"), Decimal(exercise, "carga_sugerida_kg"))).ToList()));
    }
    return Results.Ok(new TrainingPlanDetailResponse(planId, id, Text(plan, "nombre") ?? string.Empty, Date(plan, "fecha_inicio") ?? DateOnly.FromDateTime(DateTime.Today), Int(plan, "version"), days));
});
coachApi.MapPost("/advisees/{id:int}/training-plans", async (int id, CreateTrainingPlanRequest body, HttpRequest request, ClaimsPrincipal principal, SupabaseClient supabase, CancellationToken ct) =>
{
    var token = Token(request);
    var coach = await GetCoachAsync(UserId(principal), token, supabase, ct);
    if (coach is null) return Results.Forbid();
    if (string.IsNullOrWhiteSpace(body.Name) || body.Name.Trim().Length < 2 || body.Days.Count == 0) return Results.BadRequest(new { message = "Indica el nombre del plan y al menos un día de entrenamiento." });
    if (body.Days.Any(day => day.DayOfWeek is < 1 or > 7 || string.IsNullOrWhiteSpace(day.Title) || day.Exercises.Count == 0) || body.Days.Select(day => day.DayOfWeek).Distinct().Count() != body.Days.Count) return Results.BadRequest(new { message = "Cada día debe ser único y contener al menos un ejercicio." });
    if (body.Days.SelectMany(day => day.Exercises).Any(exercise => string.IsNullOrWhiteSpace(exercise.Name) || exercise.Sets is < 1 or > 20 || exercise.RepsMin < 1 || exercise.RepsMax < exercise.RepsMin || exercise.RepsMax > 1000 || exercise.RestSeconds is < 0 or > 1800 || exercise.SuggestedLoadKg is < 0 or > 1000)) return Results.BadRequest(new { message = "Uno o más ejercicios tienen datos inválidos." });

    var clients = await supabase.GetRowsAsync($"clientes?numero_cliente=eq.{id}&estado=eq.Activo&select=numero_cliente", token, ct);
    if (clients.Count == 0) return Results.NotFound(new { message = "El asesorado activo no existe." });

    var activePlans = await supabase.GetRowsAsync($"planes_entrenamiento?numero_cliente=eq.{id}&activo=eq.true&select=version", token, ct);
    var version = activePlans.OfType<JsonObject>().Select(row => Int(row, "version")).DefaultIfEmpty(0).Max() + 1;
    var insertedPlans = await supabase.PostRowsAsync("planes_entrenamiento?select=plan_id,nombre,fecha_inicio,version", new JsonObject
    {
        ["numero_cliente"] = id,
        ["coach_id"] = coach.Id,
        ["nombre"] = body.Name.Trim(),
        ["fecha_inicio"] = body.StartDate.ToString("yyyy-MM-dd"),
        // Se construye como borrador para respetar la restricción de un solo
        // plan activo por asesorado. Se activa únicamente cuando está completo.
        ["activo"] = false,
        ["version"] = version
    }, token, "return=representation", ct);
    if (insertedPlans.FirstOrDefault() is not JsonObject plan) throw new InvalidOperationException("Supabase no devolvió el plan creado.");
    var planId = Int(plan, "plan_id");

    foreach (var day in body.Days)
    {
        var insertedDays = await supabase.PostRowsAsync("plan_dias?select=plan_dia_id", new JsonObject { ["plan_id"] = planId, ["dia_semana"] = day.DayOfWeek, ["titulo"] = day.Title.Trim() }, token, "return=representation", ct);
        if (insertedDays.FirstOrDefault() is not JsonObject insertedDay) throw new InvalidOperationException("Supabase no devolvió el día del plan.");
        var planDayId = Int(insertedDay, "plan_dia_id");
        var order = 1;
        foreach (var exercise in day.Exercises)
        {
            var exerciseId = await GetOrCreateExerciseAsync(exercise.Name, day.Title, token, supabase, ct);
            await supabase.PostRowsAsync("plan_dia_ejercicios", new JsonObject
            {
                ["plan_dia_id"] = planDayId,
                ["ejercicio_id"] = exerciseId,
                ["orden"] = order++,
                ["series"] = exercise.Sets,
                ["reps_min"] = exercise.RepsMin,
                ["reps_max"] = exercise.RepsMax,
                ["descanso_seg"] = exercise.RestSeconds,
                ["carga_sugerida_kg"] = exercise.SuggestedLoadKg
            }, token, "return=minimal", ct);
        }
    }

    // El cambio de plan ocurre al final: el anterior queda como historial y el
    // borrador recién creado se vuelve el único plan activo.
    await supabase.PatchAsync($"planes_entrenamiento?numero_cliente=eq.{id}&activo=eq.true", new JsonObject { ["activo"] = false, ["fecha_fin"] = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd") }, token, ct);
    await supabase.PatchAsync($"planes_entrenamiento?plan_id=eq.{planId}", new JsonObject { ["activo"] = true }, token, ct);
    return Results.Created($"/api/coach/advisees/{id}/training-plans/{planId}", new TrainingPlanResponse(planId, id, Text(plan, "nombre") ?? body.Name.Trim(), Date(plan, "fecha_inicio") ?? DateOnly.FromDateTime(DateTime.Today), Int(plan, "version")));
});
coachApi.MapGet("/appointments", async (DateOnly from, DateOnly to, HttpRequest request, ClaimsPrincipal principal, SupabaseClient supabase, CancellationToken ct) =>
{
    if (to < from) return Results.BadRequest(new { message = "El rango de fechas no es válido." });
    var token = Token(request); var coach = await GetCoachAsync(UserId(principal), token, supabase, ct); if (coach is null) return Results.Forbid();
    var rows = await supabase.GetRowsAsync($"vw_horarios_coach?coach_id=eq.{coach.Id}&fecha=gte.{from:yyyy-MM-dd}&fecha=lte.{to:yyyy-MM-dd}&select=cita_id,fecha,hora_inicio,hora_fin,detalle,asesorado,estado&order=fecha.asc,hora_inicio.asc", token, ct);
    return Results.Ok(rows.OfType<JsonObject>().Select(ToAppointment).ToList());
});
coachApi.MapPost("/appointments", async (CreateAvailabilityRequest body, HttpRequest request, ClaimsPrincipal principal, SupabaseClient supabase, CancellationToken ct) =>
{
    if (body.DurationMinutes is < 5 or > 480 || string.IsNullOrWhiteSpace(body.Detail)) return Results.BadRequest(new { message = "Datos de horario inválidos." });
    var token = Token(request); if (await GetCoachAsync(UserId(principal), token, supabase, ct) is null) return Results.Forbid();
    var result = await supabase.RpcAsync("publicar_horario", new JsonObject { ["p_fecha"] = body.StartsAt.Date.ToString("yyyy-MM-dd"), ["p_hora_inicio"] = body.StartsAt.TimeOfDay.ToString("hh\\:mm\\:ss"), ["p_duracion_min"] = body.DurationMinutes, ["p_detalle"] = body.Detail.Trim(), ["p_modalidad"] = "Presencial" }, token, ct);
    var id = result?.GetValue<int>() ?? throw new InvalidOperationException("Supabase no devolvió el identificador de la cita.");
    var rows = await supabase.GetRowsAsync($"vw_horarios_coach?cita_id=eq.{id}&select=cita_id,fecha,hora_inicio,hora_fin,detalle,asesorado,estado", token, ct);
    return rows.FirstOrDefault() is JsonObject row ? Results.Created($"/api/coach/appointments/{id}", ToAppointment(row)) : Results.NoContent();
});
coachApi.MapPost("/appointments/{id:int}/cancel", async (int id, HttpRequest request, ClaimsPrincipal principal, SupabaseClient supabase, CancellationToken ct) =>
{
    var token = Token(request); if (await GetCoachAsync(UserId(principal), token, supabase, ct) is null) return Results.Forbid();
    await supabase.RpcAsync("cancelar_cita", new JsonObject { ["p_cita_id"] = id, ["p_reabrir"] = true }, token, ct); return Results.NoContent();
});
coachApi.MapDelete("/appointments/{id:int}", async (int id, HttpRequest request, ClaimsPrincipal principal, SupabaseClient supabase, CancellationToken ct) =>
{
    var token = Token(request); if (await GetCoachAsync(UserId(principal), token, supabase, ct) is null) return Results.Forbid();
    await supabase.RpcAsync("eliminar_horario", new JsonObject { ["p_cita_id"] = id }, token, ct); return Results.NoContent();
});

app.Run();

static string Token(HttpRequest request) => request.Headers.Authorization.ToString() is var header && header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..].Trim() : throw new UnauthorizedAccessException();
static Guid UserId(ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException());
static async Task<CoachIdentity?> GetCoachAsync(Guid userId, string token, SupabaseClient client, CancellationToken ct)
{
    var profiles = await client.GetRowsAsync($"perfiles?usuario_id=eq.{userId}&select=rol,activo", token, ct); var profile = profiles.FirstOrDefault() as JsonObject;
    if (profile is null || !Bool(profile, "activo") || !string.Equals(Text(profile, "rol"), "coach", StringComparison.OrdinalIgnoreCase)) return null;
    var coaches = await client.GetRowsAsync($"coaches?usuario_id=eq.{userId}&activo=eq.true&select=coach_id,nombre,apellidos", token, ct); var coach = coaches.FirstOrDefault() as JsonObject;
    return coach is null ? null : new CoachIdentity(Int(coach, "coach_id"), Name(coach), "coach");
}
static AnthropometricAssessmentRequest ToAssessment(JsonObject row) => new(Int(row, "mes"), Date(row, "fecha_medicion"), Decimal(row, "peso"), Decimal(row, "estatura"), Decimal(row, "pliegue_bicipital"), Decimal(row, "pliegue_tricipital"), Decimal(row, "pliegue_escapular"), Decimal(row, "pliegue_cresta_iliaca"), Decimal(row, "cintura"), Decimal(row, "cadera"), Decimal(row, "biceps"), Decimal(row, "pierna"));
static JsonObject Measurement(int client, AnthropometricAssessmentRequest value) => new() { ["numero_cliente"] = client, ["mes"] = value.MonthNumber, ["fecha_medicion"] = value.RecordedOn?.ToString("yyyy-MM-dd"), ["peso"] = value.WeightKg, ["estatura"] = value.HeightCm, ["pliegue_bicipital"] = value.BicipitalSkinfoldMm, ["pliegue_tricipital"] = value.TricipitalSkinfoldMm, ["pliegue_escapular"] = value.SubscapularSkinfoldMm, ["pliegue_cresta_iliaca"] = value.SuprailiacSkinfoldMm, ["cintura"] = value.WaistCm, ["cadera"] = value.HipCm, ["biceps"] = value.ArmCm, ["pierna"] = value.ThighCm };
static AppointmentResponse ToAppointment(JsonObject row) { var date = Text(row, "fecha") ?? throw new InvalidOperationException(); var start = DateTime.ParseExact(date + " " + Text(row, "hora_inicio"), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture); var end = DateTime.ParseExact(date + " " + Text(row, "hora_fin"), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture); var status = Text(row, "estado") switch { "Reservada" => AppointmentStatus.Booked, "Cancelada" => AppointmentStatus.Cancelled, _ => AppointmentStatus.Available }; return new AppointmentResponse(Int(row, "cita_id"), new DateTimeOffset(start, TimeSpan.Zero), Math.Max(1, (int)(end - start).TotalMinutes), Text(row, "detalle") ?? string.Empty, Text(row, "asesorado"), status); }
static int Int(JsonObject row, string property) => row[property]?.GetValue<int>() ?? 0;
static decimal? Decimal(JsonObject row, string property) => row[property]?.GetValue<decimal?>();
static bool Bool(JsonObject row, string property) => row[property]?.GetValue<bool>() ?? false;
static string? Text(JsonObject row, string property) => row[property]?.GetValue<string>();
static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
static string Name(JsonObject row) => string.Join(" ", new[] { Text(row, "nombre"), Text(row, "apellidos") }.Where(value => !string.IsNullOrWhiteSpace(value)));
static DateOnly? Date(JsonObject row, string property) => DateOnly.TryParse(Text(row, property), out var value) ? value : null;
static DateTimeOffset? Timestamp(JsonObject row, string property) => DateTimeOffset.TryParse(Text(row, property), out var value) ? value : null;
static async Task<int> GetOrCreateExerciseAsync(string name, string muscleGroup, string token, SupabaseClient supabase, CancellationToken ct)
{
    var cleanName = name.Trim();
    var found = await supabase.GetRowsAsync($"ejercicios?nombre=eq.{Uri.EscapeDataString(cleanName)}&select=ejercicio_id", token, ct);
    if (found.FirstOrDefault() is JsonObject existing) return Int(existing, "ejercicio_id");
    var inserted = await supabase.PostRowsAsync("ejercicios?select=ejercicio_id", new JsonObject { ["nombre"] = cleanName, ["grupo_muscular"] = muscleGroup, ["activo"] = true }, token, "return=representation", ct);
    if (inserted.FirstOrDefault() is JsonObject created) return Int(created, "ejercicio_id");
    throw new InvalidOperationException("Supabase no devolvió el ejercicio creado.");
}
sealed record CoachIdentity(int Id, string Name, string Role);
