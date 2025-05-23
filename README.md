# EzyTaskin

This repository contains the backend source code for EzyTaskin.

## Building

You will need the [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) to build
the code.

To build the project:

```sh
dotnet build EzyTaskin.slnx
```

## Running

Before your first run, you will need to prepare the application's runtime environment.

### PostgreSQL Database

This application uses the PostgreSQL 16 DBMS.

Running this app requires a dedicated user and database, which may be set up by running this in
a query window:

```sql
CREATE USER <UserName> WITH PASSWORD '<UserPassword>';
CREATE DATABASE <DatabaseName>;
GRANT ALL ON DATABASE <DatabaseName> TO <UserName>;
ALTER DATABASE <DatabaseName> OWNER TO <UserName>;
```

Replace `<UserName>`, `<UserPassword>`, and `<DatabaseName>` with your chosen values.

Then, we should register the connection string as a .NET
[app secret](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets):

```sh
dotnet user-secrets set ConnectionStrings:DefaultConnection \
    "Host=<HostName>; Database=<DatabaseName>; Username=<UserName>; Password=<UserPassword>" \
    --project EzyTaskin/EzyTaskin.csproj
```

### API Keys

#### Postmark

We use [Postmark](https://postmarkapp.com/) for sending emails.
For this to work, we need a
[Server Token](https://postmarkapp.com/developer/api/overview#authentication) from Postmark.

After getting one, we should register this as a .NET app secret:

```sh
dotnet user-secrets set ApiKeys:Postmark "<Postmark Server Token>" \
    --project EzyTaskin/EzyTaskin.csproj
```

#### Google/Microsoft OAuth

For external authentication providers, we need to acquire client IDs and client secrets from the
corresponding portal.

You only need to get the client ID and secret then store it using `dotnet user-secrets set`. The
other steps in the tutorials below are for app developers and are not necessary for configuration.

For Google, please follow [these instructions](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins?view=aspnetcore-8.0#create-the-google-oauth-20-client-id-and-secret).

For Microsoft, please follow [these instructions](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/microsoft-logins?view=aspnetcore-8.0#create-the-app-in-microsoft-developer-portal).


### Database Initialization

The application will need the database to have been populated with the required tables.

To do this, run:

```sh
dotnet tools install --global dotnet-ef
dotnet ef database update --project EzyTaskin/EzyTaskin.csproj
```

### Executing

After the database and API keys are ready, you can run the app using:

```sh
dotnet run --project EzyTaskin/EzyTaskin.csproj
```

## Deployment

To deploy the application, first publish the backend codebase:

```sh
dotnet publish -c Release -o out
```

For a full application with UI components included, the
[frontend static assets](https://github.com/EzyTaskin/EzyTaskin_Frontend?tab=readme-ov-file#deployment)
are required.

After exporting the frontend codebase as a static website, copy the `out` folder to the `wwwroot`
folder relative to the application binary.

```sh
cp -r /path/to/EzyTaskin_Frontend/out out/wwwroot
```
