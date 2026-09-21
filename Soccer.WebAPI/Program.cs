// View > Terminal:
// cd react.client
// npm install

using Microsoft.AspNetCore.HttpLogging;
using Soccer.Application.DependencyInjection;
using Soccer.Infrastructure.DependencyInjection;
using Soccer.Infrastructure.Persistence;
using Soccer.WebAPI.Middleware;
using System.Diagnostics;
using System.Diagnostics.Tracing;

var builder = WebApplication.CreateBuilder(args);

// >>> ЛОГУВАННЯ <<<
builder.Logging.ClearProviders(); // видаляємо дефолтні провайдери (Console, Debug)
// WebApplication.CreateBuilder(args); - автоматично реєструє кілька провайдерів:
// - Console(вивід у термінал)
// - Debug(Visual Studio Output / Debug window)
// - EventSource
// - іноді EventLog (на Windows), тобто логи вже йдуть у кілька місць одночасно.

// Console з JSON-форматтером (structured logging)
builder.Logging.AddJsonConsole(options =>
{
    // options.IncludeScopes = true; // scopes = логування контексту (наприклад, TraceId для correlation)
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
    {
        Indented = true // false = компактний JSON для production
    };
});

// для Development можна додати звичайний Console
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
}

// HTTP Logging (вбудований)
/* builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.RequestMethod
                          | HttpLoggingFields.RequestPath
                          | HttpLoggingFields.ResponseStatusCode
                          | HttpLoggingFields.Duration;

    logging.RequestHeaders.Add("User-Agent"); // додаємо User-Agent до логів, щоб бачити, з яких браузерів/клієнтів йдуть запити
    // НЕ логуємо Authorization, Cookie тощо, якби хотіли, то logging.RequestHeaders.Add("Authorization");
}); */

string firebasePath = Path.GetFullPath(
    Path.Combine(
        builder.Environment.ContentRootPath,
        "..",
        "Soccer.Infrastructure",
        "firebase.json"));

builder.Services.AddInfrastructure(firebasePath);
builder.Services.AddApplication();
builder.Services.AddControllers();

var app = builder.Build();

// ===== Middleware pipeline =====
// app.UseHttpLogging();                       // вбудований HTTP logging
app.UseMiddleware<RequestLoggingMiddleware>(); // наш middleware з duration

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<FirestoreSeeder>();
    await seeder.SeedAsync();
}

app.MapControllers();
app.Run();