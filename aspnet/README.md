# aspnet/

ASP.NET Core experiments on **.NET 10**.

> **What is ASP.NET Core?** A framework for writing programs that listen on a network port and respond to **HTTP requests** -- the same protocol browsers use to fetch web pages. You write small handler functions that say "when a request comes in for this URL, return this response." That covers everything from a simple "Hello world" page up to large JSON APIs that back mobile apps and SPAs.
>
> **What is HTTP?** A text-based request/response protocol. A client (browser, `curl`, another server) opens a connection and sends a *request* (a method like `GET`/`POST`, a URL path, headers, optional body). The server sends back a *response* (a numeric status code like `200`, headers, optional body -- usually JSON or HTML). One request → one response, then done.
>
> If you've never built a web app before, work through the lessons in order -- each one introduces one concept and explains it from first principles before showing the C# for it.

Per the repo conventions, these start with **minimal APIs** (the simplest way to declare endpoints) and graduate to **MVC controllers** (lesson 09 -- the class-based way for larger apps) and then **integration testing** (lesson 10).

## Running

Each lesson is a standalone `Microsoft.NET.Sdk.Web` project. Run with:

```pwsh
cd aspnet/01-hello-minimal/HelloMinimal
dotnet run
# console prints something like:
#   Now listening on: http://localhost:5xxx
```

Hit the endpoints with `curl`, the browser, or any REST client. Each `Program.cs` documents its endpoints in comments.

## Lesson map

| #  | Folder                          | Project              | Concept |
|----|---------------------------------|----------------------|---|
| 01 | `01-hello-minimal/`             | HelloMinimal         | `WebApplication.CreateBuilder`, `MapGet`, top-level statements |
| 02 | `02-routing/`                   | Routing              | Route params, constraints (`:int`), optional/catch-all, query strings, named routes |
| 03 | `03-json/`                      | JsonDtos             | JSON request/response with record DTOs, `Results.Created`, partial updates with `with` |
| 04 | `04-di/`                        | DiBasics             | Registering services, Singleton vs Scoped vs Transient, handler injection |
| 05 | `05-configuration/`             | ConfigOptions        | `appsettings.json`, `IConfiguration`, binding to `IOptions<T>`, env var overrides |
| 06 | `06-middleware/`                | MiddlewarePipeline   | `app.Use(...)`, short-circuiting, convention-based middleware classes, ordering |
| 07 | `07-error-handling/`            | ErrorHandling        | `AddProblemDetails`, `UseExceptionHandler`, `UseStatusCodePages`, `Results.Problem` / `ValidationProblem` |
| 08 | `08-logging/`                   | Logging              | `ILogger<T>`, structured templates, `BeginScope`, level filtering |
| 09 | `09-controllers/`               | ControllersDemo      | `AddControllers`, `[ApiController]`, `[Route]`, `CreatedAtRoute`. Exposes `Program` for testing. |
| 10 | `10-integration-tests/`         | ControllerTests      | `WebApplicationFactory<Program>` from `Microsoft.AspNetCore.Mvc.Testing`, in-process HTTP |

## Next steps (future lessons)

- Authentication & authorization (`AddAuthentication`, JWT bearer, policies)
- Data access (EF Core, repository patterns, migrations)
- Background services (`IHostedService` in the ASP.NET host)
- API versioning, OpenAPI / Swagger
- Razor Pages / Blazor / SignalR
