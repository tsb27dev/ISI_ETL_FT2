# AI Coding Guidelines for SmartGardenApi

## Project Overview
This is an ASP.NET Core Web API project targeting .NET 9, designed for smart garden management. It provides REST endpoints for garden-related data, with OpenAPI documentation.

## Architecture
- **Entry Point**: [Program.cs](Program.cs) uses minimal API style for endpoint definitions.
- **Models**: Located in [models/](models/) directory, e.g., [models/Plant.cs](models/Plant.cs) with properties like Id, Name, Location, RequiredHumidity, LastWatered.
- **Configuration**: [appsettings.json](appsettings.json) for logging and allowed hosts.

## Key Dependencies
- **ClosedXML**: For generating Excel files (e.g., exporting plant data).
- **CoreWCF**: For WCF (SOAP) services, likely for legacy system integration.
- **Newtonsoft.Json**: For JSON serialization, preferred over System.Text.Json for complex scenarios.

## Development Workflow
- **Build**: `dotnet build`
- **Run**: `dotnet run` (starts on http://localhost:5094)
- **Test Endpoints**: Use [SmartGardenApi.http](SmartGardenApi.http) with REST Client extension in VS Code.

## Conventions
- **Namespaces**: Models use `SmartGardenApi.Models`
- **Nullability**: Enabled globally; use nullable reference types.
- **Implicit Usings**: Enabled; common namespaces auto-imported.
- **API Style**: Minimal APIs in Program.cs; add endpoints directly with `app.MapGet/Post/etc.`
- **OpenAPI**: Enabled in development for automatic API docs at `/swagger`

## Patterns
- Models are simple POCOs with public getters/setters.
- For Excel exports, use ClosedXML to create workbooks and worksheets.
- If adding WCF services, configure in Program.cs with CoreWCF.

## Integration Points
- REST API with OpenAPI spec.
- Potential SOAP endpoints via CoreWCF for external systems.

This is a starting project; expand endpoints for plant CRUD operations based on Plant model.