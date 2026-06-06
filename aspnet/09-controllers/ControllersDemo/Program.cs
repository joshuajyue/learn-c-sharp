// Lesson 09: Controllers (MVC)
//
// Minimal APIs (lessons 01-08) put every endpoint in one Program.cs file as
// a lambda. Great for small apps, gets unwieldy past 20-30 endpoints.
//
// Controllers are the "classic" way: GROUP related endpoints onto one class.
// Each method on the class is an endpoint. Attributes on the class and the
// methods declare the routes and HTTP verbs. Same routing engine, same DI,
// same JSON serialization -- different layout on disk.
//
// When to use which:
//   Minimal API:  small services, prototypes, very simple endpoints.
//   Controllers:  larger APIs where grouping + shared filters + class-level
//                 helpers improve organization.
//
// Setup steps:
//   1. builder.Services.AddControllers()   -- register the MVC services
//   2. app.MapControllers()                -- scan the loaded assemblies for
//                                             controller classes & wire them up
//
// Key attributes:
//   [ApiController]
//     Opt in to "this is a JSON API controller" conventions. Most useful:
//       * Failing model validation -> automatic 400 with ProblemDetails
//       * Complex types default to [FromBody] (the JSON request body)
//       * Primitive types in routes default to [FromRoute] / [FromQuery]
//
//   [Route("api/[controller]")]
//     Declares the URL prefix for every action method. The literal
//     "[controller]" is replaced with the controller's class name minus
//     the "Controller" suffix -- so TodosController -> "api/todos".
//
//   [HttpGet], [HttpPost], [HttpPut], [HttpPatch], [HttpDelete]
//     Maps the method to that HTTP verb. They can take a route TEMPLATE
//     ("{id:int}") that's appended to the controller-level route.

using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton<TodoStore>();    // in-memory store shared by all requests

var app = builder.Build();
app.MapControllers();
app.Run();

// --- Backing store ---
public sealed class TodoStore
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<int, Todo> _data = new();
    private int _nextId;

    public IEnumerable<Todo> All() => _data.Values;
    public Todo? Get(int id) => _data.TryGetValue(id, out var t) ? t : null;
    public Todo Add(string title)
    {
        var id = Interlocked.Increment(ref _nextId);
        var todo = new Todo(id, title, Done: false);
        _data[id] = todo;
        return todo;
    }
    public bool Delete(int id) => _data.TryRemove(id, out _);
}

public record Todo(int Id, string Title, bool Done);
public record CreateTodo(string Title);

// --- The controller ---
// `ControllerBase` is the base class for API controllers (no view rendering).
// `Controller` is its subclass that adds Razor-view helpers; you only need it
// for HTML-generating endpoints.
//
// `TodoStore store` -- DI works the same as before: constructor parameters
// matching registered services are auto-injected.
[ApiController]
[Route("api/[controller]")]
public sealed class TodosController(TodoStore store) : ControllerBase
{
    // GET /api/todos
    //
    // Ok(...) returns 200 with the value serialized as JSON. The base class
    // exposes Ok/NotFound/BadRequest/NoContent/Created/CreatedAtRoute/... as
    // shortcuts for the corresponding `Results.*` you saw in minimal APIs.
    [HttpGet]
    public ActionResult<IEnumerable<Todo>> List() => Ok(store.All());

    // GET /api/todos/42
    //
    // `Name = "GetTodoById"` lets CreatedAtRoute below build the URL for us.
    [HttpGet("{id:int}", Name = "GetTodoById")]
    public ActionResult<Todo> GetById(int id)
        => store.Get(id) is { } t ? Ok(t) : NotFound();

    // POST /api/todos
    //
    // CreatedAtRoute returns 201 + a "Location" header that's the URL to GET
    // the new resource (built from the "GetTodoById" route above).
    [HttpPost]
    public ActionResult<Todo> Create(CreateTodo req)
    {
        var todo = store.Add(req.Title);
        return CreatedAtRoute("GetTodoById", new { id = todo.Id }, todo);
    }

    // DELETE /api/todos/42
    //
    // 204 No Content is the conventional "success, nothing to return" response.
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id) =>
        store.Delete(id) ? NoContent() : NotFound();
}

// Expose `Program` to the integration-test project (lesson 10).
// Top-level statements normally compile to an INTERNAL Program class -- the
// test assembly can't see it. `public partial class Program;` re-opens that
// compiler-generated class as public so WebApplicationFactory<Program> can
// find it from across the project boundary.
public partial class Program;

