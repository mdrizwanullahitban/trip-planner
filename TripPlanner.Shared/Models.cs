namespace TripPlanner.Shared;
public sealed record TripRequest(string Origin,string Destination,string GroupSize,string Budget,int DurationDays,string Transport,string Interests,string Preferences);
public sealed record Activity(string PlaceName,string Description,string BestTime,string? Address,string? MapQuery);
public sealed record DayPlan(int Day,string Title,List<Activity> Activities);
public sealed record TripPlan(string Destination,string Origin,int DurationDays,string Transport,string Budget,List<DayPlan> Itinerary,List<string> HotelSearchSuggestions,string Note);
public sealed record PlanResponse(string Resp,string Ui,TripPlan? TripPlan);