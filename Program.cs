using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);

// הגדרת מפתח JWT
var key = Encoding.ASCII.GetBytes("your_secret_key_must_be_at_least_16_chars"); 

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()   
              .AllowAnyMethod()   
              .AllowAnyHeader(); 
    });
});

var connectionString = builder.Configuration.GetConnectionString("api_todo");
builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
builder.Services.AddScoped<Service>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowAll");

//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.MapPost("/register", async (User newUser, ToDoDbContext db) =>
{
    // בדיקה בסיסית אם המשתמש כבר קיים כדי למנוע כפילויות
    var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Name == newUser.Name);
    if (existingUser != null)
    {
        return Results.Conflict("User already exists");
    }

    db.Users.Add(newUser);
    await db.SaveChangesAsync();
    
    return Results.Created($"/users/{newUser.Id}", newUser);
});

// נתיב התחברות
app.MapPost("/login", async (User loginDto, Service service) =>
{
    var user = await service.AuthenticateUser(loginDto.Name, loginDto.Password);
    
    if (user == null) return Results.Unauthorized();

    var tokenHandler = new JwtSecurityTokenHandler();
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, user.Name) }),
        Expires = DateTime.UtcNow.AddHours(1),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };
    var token = tokenHandler.CreateToken(tokenDescriptor);
    return Results.Ok(new { token = tokenHandler.WriteToken(token) });
});

// נתיבי המשימות (מוגנים על ידי RequireAuthorization)
app.MapGet("/items", async (Service service) => 
    Results.Ok(await service.GetAllItemsAsync())).RequireAuthorization();

app.MapGet("/items/{id}", async (int id, Service service) =>
{
    var item = await service.GetItemByIdAsync(id);
    return item is not null ? Results.Ok(item) : Results.NotFound("Item not found");
}).RequireAuthorization();

app.MapPost("/items", async (Item newItem, Service service) =>
{
    var createdItem = await service.CreateItemAsync(newItem);
    return Results.Created($"/items/{createdItem.Id}", createdItem);
}).RequireAuthorization();

app.MapPut("/items/{id}", async (int id, Item updatedItem, Service service) =>
{
    var success = await service.UpdateItemAsync(id, updatedItem);
    return success ? Results.NoContent() : Results.NotFound("Item not found");
}).RequireAuthorization();

app.MapDelete("/items/{id}", async (int id, Service service) =>
{
    var success = await service.DeleteItemAsync(id);
    return success ? Results.NoContent() : Results.NotFound("Item not found");
}).RequireAuthorization();

app.MapGet("/", () => "Todo API is running. Use /swagger to explore the API endpoints.");

app.Run();