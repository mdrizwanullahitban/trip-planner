# Trip Planner

A C# travel itinerary planner built with Blazor WebAssembly, ASP.NET Core Minimal API and shared .NET models.

## Requirements
.NET 9 SDK

## Run locally
Terminal 1: `dotnet run --project TripPlanner.Api --urls http://localhost:5000`

Terminal 2: `dotnet run --project TripPlanner.Client --urls http://localhost:5100`

Open http://localhost:5100.

## AI configuration
Set `OPENAI_API_KEY` as an environment variable on the API host. Do not put the key in frontend files or GitHub.

## Deployment
Publish the Blazor WebAssembly frontend as static assets to Vercel. Deploy the ASP.NET Core API to a separate .NET-compatible host and update the frontend ApiBaseUrl accordingly. Configure AllowedOrigins on the API host.

## Current limitations
Hotel suggestions are areas rather than verified live inventory. Maps open Google Maps externally. No database or user accounts in v1.
