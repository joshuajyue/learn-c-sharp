// Lesson 03: JSON in and out (REST verbs)
//
// HTTP defines several "methods" (also called "verbs") that describe what the
// request is asking the server to do. The five you'll use 95% of the time:
//
//   GET     "give me this resource"                   -- no body, safe to repeat
//   POST    "create a new resource"                   -- body usually has the new thing
//   PUT     "replace this resource completely"        -- body has the full new state
//   PATCH   "update some fields of this resource"     -- body has the changes only
//   DELETE  "remove this resource"                    -- no body
//
// JSON is the standard format for the body. It's just text shaped like a
// JavaScript object literal:
//     {"title": "buy milk", "priority": 2}
//
// ASP.NET Core handles both directions automatically:
//   * Lambda parameter of a COMPLEX TYPE -> "find the JSON body, parse it
//     into this C# object." (Called "model binding".)
//   * Lambda return value of an OBJECT  -> "serialize this to JSON, send it
//     with Content-Type: application/json."
//
// Records are perfect for these DTOs (Data Transfer Objects): immutable,
// terse, and you already met them in console lesson 04.
//
// Try:
//   curl -X POST http://localhost:5xxx/todos ^
//        -H "Content-Type: application/json" ^
//        -d "{\"title\":\"buy milk\",\"priority\":2}"

using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// In-memory store. ConcurrentDictionary because multiple requests can land at
// the same instant on different threads -- a normal Dictionary would corrupt.
var store = new ConcurrentDictionary<int, Todo>();
int nextId = 0;

// --- POST: parse a JSON body into the `CreateTodoRequest` record ---
// Results.Created(uri, value) sends 201 Created with:
//   * a "Location" header pointing at the new resource
//   * the new resource serialized as the body
app.MapPost("/todos", (CreateTodoRequest req) =>
{
    var id = Interlocked.Increment(ref nextId);
    var todo = new Todo(id, req.Title, req.Priority, Done: false);
    store[id] = todo;
    return Results.Created($"/todos/{id}", todo);
});

// --- GET one: 200 with the record (as JSON), or 404 ---
app.MapGet("/todos/{id:int}", (int id) =>
    store.TryGetValue(id, out var t) ? Results.Ok(t) : Results.NotFound());

// --- GET all: the collection is returned, framework serializes to a JSON array ---
app.MapGet("/todos", () => store.Values);

// --- PATCH: partial update. Each field on the patch DTO is nullable so
//     "null" means "don't change this field". `with` (from records) makes
//     the copy-and-modify clean. ---
app.MapPatch("/todos/{id:int}", (int id, PatchTodoRequest patch) =>
{
    if (!store.TryGetValue(id, out var existing)) return Results.NotFound();

    var updated = existing with
    {
        Title    = patch.Title    ?? existing.Title,
        Priority = patch.Priority ?? existing.Priority,
        Done     = patch.Done     ?? existing.Done,
    };
    store[id] = updated;
    return Results.Ok(updated);
});

app.Run();

// --- DTOs (Data Transfer Objects) ---
// These are the SHAPE of the JSON that goes over the wire. Keep them separate
// from your internal domain types so you can change one without breaking the
// other.
public record Todo(int Id, string Title, int Priority, bool Done);
public record CreateTodoRequest(string Title, int Priority = 1);
public record PatchTodoRequest(string? Title, int? Priority, bool? Done);

