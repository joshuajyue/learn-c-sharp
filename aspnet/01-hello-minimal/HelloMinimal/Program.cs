// Lesson 01: A web server in 6 lines
//
// What's actually happening when you run a web app:
//   1. The OS lets your program LISTEN on a TCP port (here, something like 5000).
//   2. A browser or `curl` opens a TCP connection to that port and sends an
//      HTTP REQUEST -- a text-based message that looks like:
//          GET /hello HTTP/1.1
//          Host: localhost:5000
//
//   3. Your program decides what to send back: an HTTP RESPONSE -- a status
//      code (200, 404, 500, ...), some headers, and an optional body.
//          HTTP/1.1 200 OK
//          Content-Type: text/plain
//
//          Hello, world!
//
//   4. The connection closes (or stays open for the next request).
//
// ASP.NET Core hides all of that behind one object: WebApplication. You tell
// it which URL paths ("routes") you handle and what to return. It runs the
// listener, parses requests, calls your code, and writes responses.
//
// "Minimal APIs" is the most compact way to declare those routes -- one
// `app.MapGet("/path", () => ...)` call per route. No classes, no attributes.
// Great for learning; also used in production for small services.
//
// Run:    dotnet run
// Hit:    curl http://localhost:5xxx/          (port is shown in the console)
//         curl http://localhost:5xxx/hello/Ada

var builder = WebApplication.CreateBuilder(args);
//   ^ `builder` is where you CONFIGURE the app before it starts:
//       builder.Services       -- the DI container (same one from console lesson 15)
//       builder.Configuration  -- app settings (appsettings.json, env vars, ...)
//       builder.Logging        -- log providers
//     You add things here, then call Build() to lock it in.

var app = builder.Build();
//   ^ `app` is the running application object. You use it to declare routes,
//     then call app.Run() to start the HTTP listener.

// --- Routes (endpoints) ---
// A ROUTE is "when a request matches this URL pattern + HTTP verb, run this code."
// MapGet handles GET requests. There are MapPost / MapPut / MapDelete / MapPatch too.
// The lambda's return value is auto-converted to a response:
//   string  -> text/plain
//   object  -> JSON (application/json)
//   IResult -> explicit (status code + body), via Results.* below
app.MapGet("/", () => "Hello, ASP.NET Core on .NET 10!");

// Route with a PARAMETER: anything in {curly braces} is captured and bound
// (by NAME) to a matching lambda parameter. So /hello/Ada -> name = "Ada".
app.MapGet("/hello/{name}", (string name) => new { greeting = $"Hello, {name}!" });

// Explicit status code + body. Results.* is the toolbox:
//   Results.Ok(value)        -- 200 with JSON body
//   Results.NotFound()       -- 404
//   Results.BadRequest(...)  -- 400
//   Results.StatusCode(n)    -- anything else
app.MapGet("/teapot", () => Results.StatusCode(StatusCodes.Status418ImATeapot));

app.Run();
//   ^ Starts Kestrel (the HTTP server built into ASP.NET Core). Blocks the
//     program until you press Ctrl-C. URLs come from Properties/launchSettings.json
//     by default; override with the ASPNETCORE_URLS env var or --urls.

