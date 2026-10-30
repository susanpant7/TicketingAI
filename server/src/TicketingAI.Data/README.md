# TicketingAI.Data

SQLite database layer (EF Core): entities, `AppDbContext`, configurations and migrations (the default departments and users are loaded by an admin from the app, not seeded).

The database file is created at `server/src/TicketingAI.Api/data/ticketing.db` when running locally
(`/app/data/ticketing.db` in Docker, inside the `server-data` volume).

## Database commands

Run all commands from `server/src`. Requires the EF tool: `dotnet tool install -g dotnet-ef`.

Migrations are applied automatically when the API starts, so this is only needed to apply them without running the API.

Apply migrations manually (creates the database if it does not exist):

```bash
dotnet ef database update --project TicketingAI.Data --startup-project TicketingAI.Api
```

Add a migration after changing entities or configurations:

```bash
dotnet ef migrations add <MigrationName> --project TicketingAI.Data --startup-project TicketingAI.Api --output-dir Persistence/Migrations
```

Remove the last migration (only if it has not been applied):

```bash
dotnet ef migrations remove --project TicketingAI.Data --startup-project TicketingAI.Api
```

List migrations and whether they are applied:

```bash
dotnet ef migrations list --project TicketingAI.Data --startup-project TicketingAI.Api
```

Reset the local database:

```bash
rm -rf TicketingAI.Api/data
dotnet ef database update --project TicketingAI.Data --startup-project TicketingAI.Api
```

Inspect the data:

```bash
sqlite3 TicketingAI.Api/data/ticketing.db "select * from Departments; select Id, Username, DepartmentId from Users;"
```
