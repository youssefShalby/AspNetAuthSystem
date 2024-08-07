
using AuthSystem.BLL.Settings;

var builder = WebApplication.CreateBuilder(args);

#region Services

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var smtpSettings = builder.Configuration.GetSection("SmtpSettings").Get<SmtpSettings>();
builder.Services.AddSingleton(smtpSettings);

var JWTOptions = builder.Configuration.GetSection("JWT").Get<JWT>();
builder.Services.AddSingleton(JWTOptions);

builder.Services.AddHttpContextAccessor();


string connectionString = builder.Configuration.GetConnectionString("AuthSystem");
builder.Services.AddDbContext<AppDbContext>(option => option.UseSqlServer(connectionString));

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IHandlerService, HandlerService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IRefreshTokenService, TokenService>();
builder.Services.AddScoped<IRefreshTokenRepo, RefreshTokenRepo>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddHttpContextAccessor();

//> Identity Service
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(option =>
{
	option.Password.RequiredLength = 10;
	option.User.RequireUniqueEmail = true;

	//> if tries 3 times and fail in 4th will block the account for 5 min
	option.Lockout.MaxFailedAccessAttempts = 5;
	option.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);

	//> add Entity framework implementation for Identity
}).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();

//> configure the token life time that created by Asp
builder.Services.Configure<DataProtectionTokenProviderOptions>(TokenOptions.DefaultEmailProvider, options =>
{
	options.TokenLifespan = TimeSpan.FromHours(2); // Set token lifespan to 2 hours
});

//> register service authentication
var result = builder.Services.AddAuthentication(option =>
{
	//> make authentication schema by the JWT
	option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
	option.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
});

result.AddJwtBearer(option =>
{
	option.SaveToken = true;
	option.RequireHttpsMetadata = false;

	//> create the Key, will call function later
	var theSecretKey = builder.Configuration["JWT:TokenKey"];
	var keyInBytes = Encoding.ASCII.GetBytes(theSecretKey);
	var key = new SymmetricSecurityKey(keyInBytes);

	option.TokenValidationParameters = new TokenValidationParameters()
	{
		ValidateIssuer = true,
		ValidateAudience = false,
		ValidIssuer = builder.Configuration["JWT:Issuer"],
		IssuerSigningKey = key

	};
});


//> authorization service
builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("AdminRole", builder => builder.RequireClaim(ClaimTypes.Role, "Admin"));
	options.AddPolicy("UserRole", builder => builder.RequireClaim(ClaimTypes.Role, "User"));
});

#endregion

var app = builder.Build();

#region Middlewares

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlerMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

#endregion
