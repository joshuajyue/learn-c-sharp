// Lesson 07: Error handling + ProblemDetails
//
// HTTP status codes already say "something went wrong":
//   400 Bad Request           -- the client sent something invalid
//   401 Unauthorized          -- you're not logged in
//   403 Forbidden             -- you're logged in but not allowed
//   404 Not Found             -- the URL doesn't match anything
//   409 Conflict              -- you're trying to create a duplicate
//   500 Internal Server Error -- the server crashed
//
// But just the code isn't enough -- the client also needs to know WHAT
// went wrong, in a predictable format. There's an IETF standard for that:
// "Problem Details for HTTP APIs" (RFC 7807). It's a small JSON shape:
//
//   {
//     "type":     "https://example.com/probs/divide-by-zero",
//     "title":    "Division by zero",
//     "status":   400,
//     "detail":   "Cannot divide 10 by zero",
//     "instance": "/divide?a=10&b=0",
//     "errors":   { "fieldName": [ "error message" ] }    // optional, for validation
//   }
//
// ASP.NET Core has built-in support: enable it once at startup and the
// framework will emit this shape for both your `Results.Problem(...)` calls
// AND for unhandled exceptions / bare status codes.
//
// Two switches:
//   builder.Services.AddProblemDetails();     -- registers the service that produces the JSON
//   app.UseExceptionHandler();                -- catches uncaught exceptions and converts them
//   app.UseStatusCodePages();                 -- converts bare 404/etc. into ProblemDetails JSON

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// --- An endpoint that throws. UseExceptionHandler turns this into a 500
//     with a ProblemDetails body (no leaked stack trace in production). ---
app.MapGet("/boom", () =>
{
    throw new InvalidOperationException("simulated failure");
#pragma warning disable CS0162
    return "unreachable";
#pragma warning restore CS0162
});

// --- Explicit Results.Problem(...) when YOU know the input is wrong. ---
// Use this instead of throwing -- exceptions are for unexpected failures,
// not for control flow.
app.MapGet("/divide", (int a, int b) =>
{
    if (b == 0)
    {
        return Results.Problem(
            title:  "Division by zero",
            detail: $"Cannot divide {a} by zero",
            statusCode: StatusCodes.Status400BadRequest);
    }
    return Results.Ok(new { result = a / b });
});

// --- ValidationProblem: the "your input failed validation" flavour. ---
// The convention is a dictionary keyed by field name, with one or more
// human-readable error messages per field. Frontends know how to map this
// back to the form that submitted the request.
app.MapPost("/users", (CreateUser req) =>
{
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(req.Name))
        errors["name"] = ["Name is required"];
    if (req.Age < 0)
        errors["age"]  = ["Age must be non-negative"];

    if (errors.Count > 0) return Results.ValidationProblem(errors);
    return Results.Created($"/users/{req.Name}", req);
});

// --- Bare 404 (e.g. /nope) is now also formatted as ProblemDetails JSON
//     thanks to UseStatusCodePages above. ---

app.Run();

public record CreateUser(string Name, int Age);

