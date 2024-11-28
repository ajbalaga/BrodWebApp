using System.Text;
using BrodClientAPI.Controller;
using BrodClientAPI.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure MongoDB settings
builder.Services.AddSingleton<IMongoClient, MongoClient>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("MongoDbConnection");
    var mongoClientSettings = MongoClientSettings.FromConnectionString(connectionString);
    mongoClientSettings.MaxConnectionPoolSize = 500;// Ensure this is in your appsettings.json
    mongoClientSettings.MinConnectionPoolSize = 50;  // Optional: Set a minimum pool size
    return new MongoClient(mongoClientSettings);
});

// Register the custom MongoDB context
builder.Services.AddScoped<ApiDbContext>();

// Add Controllers
builder.Services.AddControllers();
builder.Services.AddMemoryCache();

// Configure CORS to allow any origin, header, and method
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllCORS", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowSpecificOrigins", builder =>
//    {
//        builder.WithOrigins("https://webapp.brod.com.au")  // Allow your frontend domain
//               .AllowAnyHeader()                          // Allow any header
//               .AllowAnyMethod();                         // Allow any method
//    });
//});

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Brod Client API", Version = "v1" });

    // Configure Swagger to use JWT Bearer tokens
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Please enter your JWT token here"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

// Configure JWT authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]))
    };
});

// Configure Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ClientPolicy", policy => policy.RequireRole("Client"));
    options.AddPolicy("TradiePolicy", policy => policy.RequireRole("Tradie"));
});


// Register controllers
builder.Services.AddScoped<AdminController>();
builder.Services.AddScoped<AuthController>();
builder.Services.AddScoped<ClientController>();
builder.Services.AddScoped<TradieController>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseCors("AllowAllCORS");
//app.UseCors("AllowSpecificOrigins");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DefaultModelsExpandDepth(-1);
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Brod Client API V1");
    });
}
else if (app.Environment.IsProduction())
{
    // Optionally enable Swagger in production (not recommended for public APIs without proper security)
    app.UseSwagger(); // Enable Swagger in production only if required
    app.UseSwaggerUI(options =>
    {
        options.DefaultModelsExpandDepth(-1);
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Brod Client API V1");
    });

    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication(); // Ensure this is before UseAuthorization
app.UseAuthorization();


app.MapControllers();

app.Run();
