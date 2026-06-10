var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5223");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors("AllowLocalhost");
app.UseAuthorization();
app.MapControllers();

app.Run();
