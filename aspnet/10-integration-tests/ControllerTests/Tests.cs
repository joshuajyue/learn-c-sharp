// Lesson 10: Integration tests with WebApplicationFactory<TProgram>
//
// You've seen UNIT tests (exercises/...) that test one method in isolation.
// An INTEGRATION test exercises a real slice of the system end-to-end -- in
// this case, it boots the entire web app and sends real HTTP requests to it.
//
// You COULD start the app, hit ports with curl, and shell-script assertions.
// That's slow, fragile, and depends on free TCP ports. ASP.NET Core ships
// `WebApplicationFactory<T>` for a better approach:
//
//   * Boots your app IN-PROCESS using the exact same Program.cs as `dotnet run`
//   * Gives you an HttpClient that talks to it WITHOUT any TCP / port binding
//   * Lets you override services for the test run (e.g. fake clock, in-mem DB)
//
// Tests run as fast as ordinary xUnit tests. Same `dotnet test` workflow.
//
// Pre-req: the target Program class has to be PUBLIC so this assembly can
// reference it. Lesson 09 ends with `public partial class Program;` exactly
// for this.

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ControllerTests;

// `IClassFixture<T>` is xUnit's "share one instance of T across all tests in
// this class". So the web app boots ONCE and every test reuses it -- fast.
public class TodosControllerTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    // factory.CreateClient() returns an HttpClient pre-configured with the
    // base address of the in-memory app. .GetAsync("/api/todos") works
    // exactly like talking to a real server.
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GET_list_returns_200()
    {
        var resp = await _client.GetAsync("/api/todos");
        resp.EnsureSuccessStatusCode();                       // throws if not 2xx
        var items = await resp.Content.ReadFromJsonAsync<Todo[]>();
        Assert.NotNull(items);
        // (May not be empty if other tests ran first -- the store is a singleton
        //  and tests share it. Asserting "no error + valid JSON" is enough here.)
    }

    [Fact]
    public async Task POST_then_GET_round_trip()
    {
        // PostAsJsonAsync serializes the anonymous object to JSON and sets
        // Content-Type: application/json. This is what the test "client" does
        // -- the server then deserializes it into a `CreateTodo` record.
        var create = await _client.PostAsJsonAsync("/api/todos", new { title = "buy milk" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var created = await create.Content.ReadFromJsonAsync<Todo>();
        Assert.NotNull(created);
        Assert.Equal("buy milk", created!.Title);

        var get = await _client.GetAsync($"/api/todos/{created.Id}");
        get.EnsureSuccessStatusCode();
        var fetched = await get.Content.ReadFromJsonAsync<Todo>();
        Assert.Equal(created, fetched);     // value equality comes free with records
    }

    [Fact]
    public async Task GET_missing_returns_404()
    {
        var resp = await _client.GetAsync("/api/todos/999999");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task DELETE_then_GET_returns_404()
    {
        var create = await _client.PostAsJsonAsync("/api/todos", new { title = "temp" });
        var created = await create.Content.ReadFromJsonAsync<Todo>();

        var del = await _client.DeleteAsync($"/api/todos/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var get = await _client.GetAsync($"/api/todos/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }
}

// Local DTO mirror so we don't have to expose the controller's record across
// the project boundary. In a real solution you'd put DTOs in a shared
// "Contracts" project that both the API and the tests reference.
public record Todo(int Id, string Title, bool Done);

