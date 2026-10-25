# Trip Planner — Implementation Plan

C# full-stack travel planner built with Blazor WebAssembly and ASP.NET Core.

## MVP
- Eight-step questionnaire: origin, destination, group, budget, duration, transport, interests, preferences.
- Default transport: Four Wheeler. Bike rental disabled.
- Structured AI itinerary generation through a server-side API.
- Day-by-day results, hotel-area suggestions, Google Maps links.
- Responsive design, input validation, error handling.

## Architecture
- TripPlanner.Client: Blazor WebAssembly, deployed as static output to Vercel.
- TripPlanner.Api: ASP.NET Core Minimal API, hosted separately.
- TripPlanner.Shared: request/response models.
- No database in v1; local persistence is optional.

## Security
Never commit API keys. Store OPENAI_API_KEY on the API host.

## Build sequence
1. Scaffold .NET solution and projects.
2. Implement questionnaire and responsive UI.
3. Add API endpoint and AI JSON schema.
4. Test end-to-end locally.
5. Publish frontend and backend.
