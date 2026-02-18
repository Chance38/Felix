var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();

// TODO: Add Domain services
// builder.Services.AddDomain();

// TODO: Add Infrastructure services
// builder.Services.AddInfrastructure();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Health check endpoint
app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();
