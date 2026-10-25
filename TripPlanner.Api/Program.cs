using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TripPlanner.Shared;
var builder=WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
var origins=builder.Configuration["AllowedOrigins"]?.Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries)??new[]{"http://localhost:5100"};
builder.Services.AddCors(o=>o.AddPolicy("client",p=>p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));
var app=builder.Build();
app.UseCors("client");
app.MapGet("/api/health",()=>Results.Ok(new {status="ok"}));
app.MapPost("/api/plan",async (TripRequest request,IHttpClientFactory factory,IConfiguration config,CancellationToken ct)=>{
 if(string.IsNullOrWhiteSpace(request.Origin)||string.IsNullOrWhiteSpace(request.Destination)||request.DurationDays is <1 or >21)
  return Results.BadRequest(new {error="Invalid origin, destination or duration"});
 var key=config["OPENAI_API_KEY"];
 if(string.IsNullOrWhiteSpace(key))return Results.Problem("AI is not configured",statusCode:503);
 const string schema="""{"type":"object","additionalProperties":false,"properties":{"resp":{"type":"string"},"ui":{"type":"string"},"tripPlan":{"type":"object","additionalProperties":false,"properties":{"destination":{"type":"string"},"origin":{"type":"string"},"durationDays":{"type":"integer"},"transport":{"type":"string"},"budget":{"type":"string"},"itinerary":{"type":"array","items":{"type":"object","additionalProperties":false,"properties":{"day":{"type":"integer"},"title":{"type":"string"},"activities":{"type":"array","items":{"type":"object","additionalProperties":false,"properties":{"placeName":{"type":"string"},"description":{"type":"string"},"bestTime":{"type":"string"},"address":{"type":["string","null"]},"mapQuery":{"type":["string","null"]}},"required":["placeName","description","bestTime","address","mapQuery"]}}},"required":["day","title","activities"]}},"hotelSearchSuggestions":{"type":"array","items":{"type":"string"}},"note":{"type":"string"}},"required":["destination","origin","durationDays","transport","budget","itinerary","hotelSearchSuggestions","note"]}},"required":["resp","ui","tripPlan"]}""";
 var prompt="Generate a realistic travel itinerary in JSON with ui=final. Do not invent verified hotel prices, ratings, availability, ticket prices or travel times. Suggest hotel areas, not unverified hotel names. Provide 2-4 activities per day and Google Maps search queries. Include a note to verify logistics.";
 var payload=new {model=config["OPENAI_MODEL"]??"gpt-4.1-mini",messages=new[]{new {role="system",content=prompt},new {role="user",content=JsonSerializer.Serialize(request)}},response_format=new {type="json_schema",json_schema=new {name="trip_plan",strict=true,schema=JsonSerializer.Deserialize<JsonElement>(schema)}}};
 using var message=new HttpRequestMessage(HttpMethod.Post,"https://api.openai.com/v1/chat/completions");
 message.Headers.Authorization=new AuthenticationHeaderValue("Bearer",key);
 message.Content=new StringContent(JsonSerializer.Serialize(payload),Encoding.UTF8,"application/json");
 try {
  using var response=await factory.CreateClient().SendAsync(message,ct);
  if(!response.IsSuccessStatusCode)return Results.Problem("AI request failed",statusCode:502);
  using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
  var content=json.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
  var result=JsonSerializer.Deserialize<PlanResponse>(content!,new JsonSerializerOptions{PropertyNameCaseInsensitive=true});
  return result is null?Results.Problem("Invalid AI response",statusCode:502):Results.Ok(result);
 }catch(Exception){return Results.Problem("Could not generate itinerary",statusCode:502);}
});
app.Run();