using TodoApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()   
              .AllowAnyMethod()   
              .AllowAnyHeader(); 
    });
});

builder.Services.AddDbContext<ToDoDbContext>();
builder.Services.AddScoped<Service>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.MapGet("/items", async (Service service) => 
    Results.Ok(await service.GetAllItemsAsync()));

app.MapGet("/items/{id}", async (int id, Service service) =>
{
    var item = await service.GetItemByIdAsync(id);
    return item is not null ? Results.Ok(item) : Results.NotFound("Item not found");
});

app.MapPost("/items", async (Item newItem, Service service) =>
{
    var createdItem = await service.CreateItemAsync(newItem);
    return Results.Created($"/items/{createdItem.Id}", createdItem);
});

app.MapPut("/items/{id}", async (int id, Item updatedItem, Service service) =>
{
    var success = await service.UpdateItemAsync(id, updatedItem);
    return success ? Results.NoContent() : Results.NotFound("Item not found");
});

app.MapDelete("/items/{id}", async (int id, Service service) =>
{
    var success = await service.DeleteItemAsync(id);
    return success ? Results.NoContent() : Results.NotFound("Item not found");
});




app.Run();

