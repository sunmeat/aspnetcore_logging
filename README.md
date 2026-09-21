# Clean Architecture + React + Firebase + Logging

Навчальний приклад вебзастосунку на **ASP.NET Core Web API + React + Firebase Cloud Firestore**, побудованого за принципами **Clean Architecture**, з повноцінним логуванням через стандартний фреймворк **Microsoft.Extensions.Logging**.

Проєкт є продовженням [aspnetcore_firebase_clean](https://github.com/sunmeat/aspnetcore_firebase_clean) і демонструє, як додати структуроване логування без сторонніх бібліотек (Serilog, NLog тощо).

---

## Технології

### Backend
- C# / .NET 10
- ASP.NET Core Web API
- Clean Architecture
- Firebase Cloud Firestore
- Microsoft.Extensions.Logging
- LoggerMessage source generation
- HTTP Logging / custom middleware
- Dependency Injection
- Repository Pattern
- DTO + async/await

### Frontend
- React 19
- Vite
- JavaScript

### Database
Cloud Firestore (колекції `teams` та `players`).

---

## Що логується

| Шар | Що логуємо | Засоби |
|-----|------------|--------|
| **WebAPI** | HTTP-метод, path, status code, тривалість | `RequestLoggingMiddleware`, `ILogger<T>` |
| **Application** | Бізнес-події (створення/оновлення/видалення, «не знайдено») | `ILogger<T>`, `LoggerMessage` |
| **Infrastructure** | Запити до Firestore, кількість документів, технічні помилки | `ILogger<T>`, `LoggerMessage` |
| **Domain** | Нічого | Шар залишається чистим |

---

## Рівні логування

| Рівень | Призначення |
|--------|-------------|
| **Trace** | Найдетальніша інформація (глибоке налагодження) |
| **Debug** | Технічна інформація під час розробки |
| **Information** | Нормальні значущі події |
| **Warning** | Нетипова ситуація, система продовжує працювати |
| **Error** | Операція завершилася помилкою |
| **Critical** | Серйозна проблема, що може зупинити систему |

Рівні налаштовуються в `appsettings.json` / `appsettings.Development.json` і дозволяють фільтрувати шум.

---

## Structured logging + EventId

Логи пишуться у структурованому вигляді. Приклад:

```json
{
  "Timestamp": "2026-09-21T14:26:57.588Z",
  "EventId": 1005,
  "LogLevel": "Information",
  "Category": "Soccer.Application.Services.PlayerService",
  "Message": "Player 27 deleted",
  "State": {
    "PlayerId": 27
  }
}
```

`EventId` однозначно ідентифікує тип події:

| EventId | Подія |
|---------|-------|
| 1001 | PlayerCreated |
| 1002 | PlayerNotFound |
| 1003 | PlayerCreationFailed |
| 1004 | PlayerUpdated |
| 1005 | PlayerDeleted |
| 1101 | TeamCreated |
| 1102 | TeamNotFound |
| 1103 | TeamCreationFailed |
| 2001 | FirestoreQueryStarted |
| 2002 | FirestoreQueryCompleted |
| 2003 | FirestoreOperationFailed |
| 2004 | DocumentCreated |
| 2005 | DocumentDeleted |

---

## LoggerMessage (source generation)

Для часто виконуваних логів використовується source generation:

```csharp
public static partial class ApplicationLogMessages
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
        Message = "Player {PlayerId} created")]
    public static partial void PlayerCreated(this ILogger logger, int playerId);
}
```

Виклик у сервісі:

```csharp
_logger.PlayerCreated(player.Id);
```

Код генерується під час компіляції — без зайвих алокацій і з повною підтримкою structured logging.

---

## Обробка винятків

Exception передається окремим параметром:

```csharp
try
{
    await players.Create(player);
    _logger.PlayerCreated(player.Id);
}
catch (Exception ex)
{
    _logger.PlayerCreationFailed(ex, player.Id); // stack trace зберігається
    throw;
}
```

Logging infrastructure отримує повідомлення, stack trace, inner exception і тип винятку.

---

## HTTP-логування

Кастомний middleware фіксує:

```
HTTP GET /api/players responded 200 in 115 ms
HTTP DELETE /api/players/27 responded 204 in 210 ms
```

Логуються: HTTP method, path, status code, duration.  
Sensitive headers (Authorization, Cookie) не записуються.

---

## Структура solution

```
Soccer
│
├── Soccer.Domain              ← Entities + Interfaces (без логування)
│
├── Soccer.Application
│   ├── Services               ← бізнес-події + ILogger
│   └── Logging
│       └── ApplicationLogMessages.cs
│
├── Soccer.Infrastructure
│   ├── Repositories           ← Firestore + ILogger
│   └── Logging
│       └── InfrastructureLogMessages.cs
│
├── Soccer.Common
│   └── Exceptions
│
├── Soccer.WebAPI
│   ├── Middleware
│   │   └── RequestLoggingMiddleware.cs
│   ├── Controllers
│   ├── Program.cs             ← налаштування провайдерів і рівнів
│   └── appsettings*.json
│
└── react.client               ← React + Vite
```

---

## Налаштування логування (Program.cs)

```csharp
builder.Logging.ClearProviders();

if (builder.Environment.IsDevelopment())
{
    // Зручний текстовий вивід для розробки
    builder.Logging.AddConsole();
}
else
{
    // Структурований JSON для production
    builder.Logging.AddJsonConsole(options =>
    {
        options.IncludeScopes = false;
        options.TimestampFormat = "HH:mm:ss ";
        options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
        {
            Indented = false
        };
    });
}
```

### appsettings.Development.json (рекомендовані рівні)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "Soccer": "Debug"
    }
  }
}
```

Це прибирає шум від Routing, MVC, Hosting.Diagnostics і залишає лише корисні логи.

---

## Приклад чистого виводу (Development)

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5000

dbug: Soccer.Infrastructure.Repositories.PlayerRepository[2001]
      Firestore query started for collection players
dbug: Soccer.Infrastructure.Repositories.TeamRepository[2001]
      Firestore query started for collection teams
dbug: Soccer.Infrastructure.Repositories.PlayerRepository[2002]
      Firestore query completed for collection players. Documents: 26
info: Soccer.Application.Services.PlayerService[0]
      Retrieved 26 players
info: Soccer.WebAPI.Middleware.RequestLoggingMiddleware[0]
      HTTP GET /api/players responded 200 in 115 ms

info: Soccer.Infrastructure.Repositories.PlayerRepository[2005]
      Document 27 deleted from collection players
info: Soccer.Application.Services.PlayerService[1005]
      Player 27 deleted
info: Soccer.WebAPI.Middleware.RequestLoggingMiddleware[0]
      HTTP DELETE /api/players/27 responded 204 in 210 ms
```

---

## Firebase credentials

Для локального запуску потрібен Service Account JSON:

1. Firebase Console → Project settings → Service accounts  
2. Generate new private key  
3. Покласти файл у:

```
Soccer.Infrastructure/firebase.json
```

Файл **не повинен** потрапляти в Git (вже є в `.gitignore`).

---

## Запуск

```bash
# 1. Клонування
git clone https://github.com/sunmeat/aspnetcore_logging.git
cd aspnetcore_logging

# 2. Backend
dotnet restore
dotnet build
dotnet run --project Soccer.WebAPI

# 3. Frontend (окремий термінал)
cd react.client
npm install
npm run dev
```

API:
- `GET/POST/PUT/DELETE /api/players`
- `GET/POST/PUT/DELETE /api/teams`

---

## Redaction (чутливі дані)

У проєкті закладено можливість маскувати персональні дані (наприклад, email гравця або ім’я тренера) через `Microsoft.Extensions.Compliance.Redaction`.

Поле позначається класифікацією (`DataClassification`), після чого значення автоматично маскується або замінюється на `[REDACTED]` перед записом у лог. Це дозволяє зберігати корисність логів без витоку конфіденційної інформації.

---

## Навчальна мета

Проєкт показує:

- як інтегрувати стандартне логування .NET у Clean Architecture
- як розділити відповідальність логування між шарами
- як використовувати EventId і LoggerMessage
- як писати structured logs
- як налаштувати рівні, щоб уникнути log noise
- як логувати HTTP-запити без витоку sensitive data

Domain залишається незалежним від механізму логування.
