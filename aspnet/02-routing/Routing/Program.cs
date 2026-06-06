// Lesson 02: Routing -- how a URL gets matched to your code
//
// Recap: when an HTTP request comes in, it has a METHOD (GET/POST/...) and a
// URL PATH (the part after the host: e.g. "/orders/42"). The router's job is
// to look at both and decide which of your handlers (if any) should run.
//
// A ROUTE TEMPLATE is a pattern with literal segments and {placeholders}:
//
//     "/orders/{id:int}"
//      ^^^^^^^^ literal -- must match exactly
//               ^^^^^^^^ placeholder -- captures a path segment into a variable.
//                        :int is a CONSTRAINT: the segment must parse as int.
//
// If no route matches the incoming URL, ASP.NET Core sends back 404 Not Found.
//
// Template features:
//   {param}              required path segment, bound to a lambda param by NAME
//   {param:int}          add a type/format constraint (404 if it doesn't parse)
//   {param?}             optional path segment
//   {*rest}              "catch-all": matches the rest of the path (including /'s)
//
// Where do values come from?
//   In path template      -> route value (from {curly braces})
//   Not in path template  -> query string (?key=value at the end of the URL)
//   Complex type          -> JSON body of the request (covered in lesson 03)

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// --- Path parameter with a type constraint ---
// /orders/42  -> 200 OK, JSON { "id": 42, "status": "shipped" }
// /orders/abc -> 404 Not Found  (doesn't match the :int constraint, so the
//                                router never even calls this handler)
app.MapGet("/orders/{id:int}", (int id) => new { id, status = "shipped" });

// --- Multiple constraints stacked together ---
// :int and :min(1) and :max(100) all have to pass.
app.MapGet("/page/{n:int:min(1):max(100)}", (int n) => $"page {n}");

// --- Optional segment ---
// /users        -> name is null  -> "all users"
// /users/ada    -> name = "ada"  -> "user ada"
app.MapGet("/users/{name?}",
    (string? name) => name is null ? "all users" : $"user {name}");

// --- Query strings ---
// A query string is the `?a=1&b=2` part at the end of a URL. Any parameter
// that's NOT in the route template is bound from there by name.
// /search?q=hello              -> q = "hello", limit = 20 (default)
// /search?q=hello&limit=5      -> q = "hello", limit = 5
app.MapGet("/search",
    (string q, int limit = 20) => new { q, limit, hits = Array.Empty<string>() });

// --- Catch-all parameter (* or **) ---
// Matches one or more segments greedily.
// /files/a/b/c.txt -> path = "a/b/c.txt"
app.MapGet("/files/{*path}", (string path) => new { path });

// --- Naming a route so you can build URLs to it later ---
// In lesson 09 you'll see CreatedAtRoute("GetProductById", ...) build the
// "/products/42" URL automatically using this name.
app.MapGet("/products/{id:int}", (int id) => new { id, name = $"product {id}" })
   .WithName("GetProductById");

app.Run();

