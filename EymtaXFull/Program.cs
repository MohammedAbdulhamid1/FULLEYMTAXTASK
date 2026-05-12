using Eymta.Application.Interfaces;
using Eymta.Application.Mappings;
using Eymta.Application.Validators;
using Eymta.core.Interface;
using Eymta.Repository.Data;
using Eymta.Repository.Data.Seeding;
using Eymta.Repository.Repositories;
using Eymta.Repository.Services;
using EymtaXFull.Hubs;
using EymtaXFull.Middlewares;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;


namespace EymtaXFull
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ── Database (PostgreSQL / Supabase) ──────────────────────
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    sql => sql.MigrationsAssembly("Eymta.Repository")));

            // ── Repository & Unit of Work ─────────────────────────────
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // ── Application Services ──────────────────────────────────
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<ITeamService, TeamService>();
            builder.Services.AddScoped<ITaskService, TaskService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddSignalR();

            // ── AutoMapper ────────────────────────────────────────────
            builder.Services.AddAutoMapper(typeof(MappingProfile));

            // ── FluentValidation ──────────────────────────────────────
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

            // ── JWT Authentication ────────────────────────────────────
            var jwtKey = builder.Configuration["JWT:Key"]
                ?? throw new InvalidOperationException("JWT:Key is not configured.");

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                        ValidateIssuer = true,
                        ValidIssuer = builder.Configuration["JWT:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = builder.Configuration["JWT:Audience"],
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };

                    // SignalR JWT support
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            if (!string.IsNullOrEmpty(accessToken) &&
                                path.StartsWithSegments("/teamChatHub"))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddAuthorization();

            // ── Controllers ───────────────────────────────────────────
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy =
                        System.Text.Json.JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.ReferenceHandler =
                        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                });

            // ── CORS ──────────────────────────────────────────────────
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("EymtaXPolicy", policy =>
                {
                    policy.WithOrigins(
                        builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                        ?? new[]
                        {
                            "http://localhost:5173",
                            "http://localhost:3000",
                            "http://127.0.0.1:5500",
                            "http://localhost:5500"
                        })
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            // ── Swagger (always enabled for Render deployment) ────────
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Eymta X – Task Management API",
                    Version = "v1",
                    Description = "Production-ready REST API for the Eymta X Task Management System"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter: Bearer {your JWT token}"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id   = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // ── Static files (for attachments) ────────────────────────
            builder.Services.AddDirectoryBrowser();

            // ── Port config for Render ────────────────────────────────
            var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
            builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

            // ════════════════════════════════════════════════════════════
            var app = builder.Build();
            // ════════════════════════════════════════════════════════════

            // ── Global error logger ───────────────────────────────────
            app.Use(async (context, next) =>
            {
                try { await next(); }
                catch (Exception ex)
                {
                    Console.WriteLine($"UNHANDLED: {ex.Message}\n{ex.StackTrace}");
                    throw;
                }
            });

            // ── Apply migrations & seed ───────────────────────────────
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.MigrateAsync();
                await DataSeeder.SeedAsync(db);
            }

            // ── Middleware pipeline ───────────────────────────────────
            app.UseMiddleware<ExceptionMiddleware>();

            // Swagger always enabled (needed on Render)
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseStaticFiles();
            app.UseCors("EymtaXPolicy");
            app.UseAuthentication();
            app.UseMiddleware<ActivityTrackingMiddleware>();
            app.UseAuthorization();

            app.Use(async (context, next) =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Request.Headers["Authorization"] = $"Bearer {accessToken}";
                }
                await next();
            });

            app.MapHub<TeamChatHub>("/teamChatHub");
            app.MapControllers();

            app.Run();
        }
    }
}
