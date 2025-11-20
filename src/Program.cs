using HSB.BE.Data;
using HSB.BE.Models;
using HSB.BE.Repository;
using HSB.BE.Services;
using HSB.BE.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace HSB.BE
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			builder.Services.AddDbContext<AppDbContext>(options =>
				options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

			builder.Services.AddScoped<IUserRepository, UserRepository>();
			builder.Services.AddScoped<IAuthService, AuthService>();
			builder.Services.AddScoped<ITokenService, TokenService>();
			builder.Services.AddScoped<IEmailService, EmailService>();

			// Add authentication
			builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

			builder.Services
				.AddAuthentication(options =>
				{
					options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
					options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
				})
				.AddJwtBearer(options =>
				{
					options.RequireHttpsMetadata = true; // true in prod, change false for testing locally
					options.SaveToken = true;

					options.TokenValidationParameters = new TokenValidationParameters
					{
						ValidateIssuerSigningKey = true,
						IssuerSigningKey = new SymmetricSecurityKey(
							Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),

						ValidateIssuer = !string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Issuer"]),
						ValidIssuer = builder.Configuration["Jwt:Issuer"],

						ValidateAudience = !string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Audience"]),
						ValidAudience = builder.Configuration["Jwt:Audience"],

						ValidateLifetime = true,
						ClockSkew = TimeSpan.FromMinutes(1)
					};
				});

			builder.Services
			.AddOptions<AzureOpenAIOptions>() // returns OptionsBuilder<AzureOpenAIOptions>
			.Bind(builder.Configuration.GetSection("AzureOpenAI"))
			.ValidateDataAnnotations() // Throws an error if necessary appsettings or user secrets are missing.
			.ValidateOnStart(); // Makes sure the validation happens on startup instead of lazily.

			builder.Services
			.AddOptions<CosmosDbOptions>()
			.Bind(builder.Configuration.GetSection("CosmosDb"))
			.ValidateDataAnnotations()
			.ValidateOnStart();

			builder.Services.AddSingleton<IConversationMemoryService, ConversationMemoryService>();

			builder.Services.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			});
			builder.Services.AddAuthorization();

			builder.Services.AddControllers();

			builder.Services.AddControllers();
			// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
			builder.Services.AddEndpointsApiExplorer();

			builder.Services.AddSwaggerGen(c =>
			{
				c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
				{
					Type = SecuritySchemeType.Http,
					Scheme = "bearer",
					BearerFormat = "JWT",
					In = ParameterLocation.Header,
					Name = "Authorization"
				});

				c.AddSecurityRequirement(new OpenApiSecurityRequirement
				{
					{
						new OpenApiSecurityScheme
						{
							Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
						},
						Array.Empty<string>()
					}
				});
			});

			// Get CORS origins from config
			var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
			builder.Services.AddCors(options =>
			{
				options.AddPolicy("AllowSpecificOrigins", policy =>
				{
					policy
						.WithOrigins(allowedOrigins!)
						.AllowAnyHeader()
						.AllowAnyMethod();
				});
			});

			var app = builder.Build();

			app.UseSwagger();
			app.UseSwaggerUI();

			app.UseHttpsRedirection();

			app.UseCors("AllowSpecificOrigins");

			app.UseAuthentication();
			app.UseAuthorization();

			app.MapControllers();

			app.Run();
		}
	}
}