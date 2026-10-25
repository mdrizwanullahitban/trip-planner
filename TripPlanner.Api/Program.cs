using System.Text;
using System.Text.Json;
using TripPlanner.Shared;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
var origins = builder.Configuration["AllowedOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? new[] { "http://localhost:5100" };
builder.Services.AddCors(options => options.AddPolicy("client", policy =>
    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
app.UseCors("client");
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", provider = "gemini" }));

app.MapPost("/api/plan", async (TripRequest request, IHttpClientFactory factory, IConfiguration config, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Origin) || string.IsNullOrWhiteSpace(request.Destination)
        || request.DurationDays is < 1 or > 21)
        return Results.BadRequest(new { error = "Please provide valid trip details." });

    var key = config["GEMINI_API_KEY"];
    if (string.IsNullOrWhiteSpace(key))
        return Results.Problem("Gemini API key is not configured on the server.", statusCode: 503);

    var model = config["GEMINI_MODEL"] ?? "gemini-2.5-flash";
    var prompt = """
        You are a careful travel planner. Return a JSON object matching exactly this structure:
        {
          "resp":"Friendly one-sentence summary",
          "ui":"final",
          "tripPlan":{
            "destination":"Destination","origin":"Origin","durationDays":3,
            "transport":"Four Wheeler","budget":"Medium",
            "itinerary":[{"day":1,"title":"Day theme","activities":[
              {"placeName":"Place","description":"Why visit","bestTime":"Morning",
               "address":null,"mapQuery":"Place, Destination"}]}],
            "hotelSearchSuggestions":["Neighbourhood or area to look for accommodation"],
            "note":"Estimates only; verify routes, opening hours, prices and availability."
          }
        }
        Generate exactly the requested number of days, with 2-4 sensible activities each day.
        Account for the chosen transport, interests, group and budget. Suggest hotel AREAS,
        not unverified live hotel listings. Do not invent verified prices, ratings, opening hours,
        booking availability, or driving times. Output JSON only.
        """;

    var payload = new
    {
        systemInstruction = new { parts = new[] { new { text = prompt } } },
        contents = new[] { new { role = "user", parts = new[] { new { text = JsonSerializer.Serialize(request) } } } },
        generationConfig = new { responseMimeType = "application/json", temperature = 0.5 }
    };

    using var message = new HttpRequestMessage(HttpMethod.Post,
        $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent");
    message.Headers.Add("x-goog-api-key", key);
    message.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    try
    {
        using var response = await factory.CreateClient().SendAsync(message, ct);
        if (!response.IsSuccessStatusCode)
        {
            app.Logger.LogWarning("Gemini returned HTTP {StatusCode}. Verify model, key and quota.", (int)response.StatusCode);
            return Results.Problem(response.StatusCode switch { System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden => "Gemini rejected the API key or its permissions.", System.Net.HttpStatusCode.TooManyRequests => "Gemini rate limit or quota reached. Please try later or check your Google AI Studio quota.", System.Net.HttpStatusCode.NotFound => $"Gemini model '{model}' is unavailable. Check GEMINI_MODEL.", _ => $"Gemini returned HTTP {(int)response.StatusCode}. Check the backend logs." }, statusCode: 502);
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var text = document.RootElement.GetProperty("candidates")[0]
            .GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
        if (string.IsNullOrWhiteSpace(text))
            return Results.Problem("Gemini returned an empty itinerary.", statusCode: 502);

        var plan = JsonSerializer.Deserialize<PlanResponse>(text,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (plan?.TripPlan is null || plan.TripPlan.Itinerary is null || plan.TripPlan.Itinerary.Count == 0)
            return Results.Problem("Gemini returned an incomplete itinerary.", statusCode: 502);
        return Results.Ok(plan);
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested)
    {
        return Results.StatusCode(499);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Gemini itinerary generation failed");
        return Results.Problem("Could not generate itinerary. Please try again.", statusCode: 502);
    }
});
app.Run();
