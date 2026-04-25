# Koreader Sync Server Extended

Re-implementation and extension of koreader sync server while retaining full compatibility with default implementation.

Extensions and differences:
- Uses EF Core with flexible choice of database provider.
- Uses ASP.NET Core Identity to store user password more securely instead of just relying on client-side md5 hash.
- Uses ASP.NET Core Configuration and Options with flexible choice of providers.
    - Enable/disable user registration at run time without restarting server (if configuration provider supports reloading).
- Uses ASP.NET Core Logging with flexible choice of provider.
- CLI command to create users by directly interacting with database even while user registration is disabled.
- Health check will check database status instead of always returning 200.
- Delete progress at `DELETE syncs/progress/{document}`
- Delete all progress at `DELETE syncs/progress`
- Get all progress at `GET syncs/progress`
- Change password at `POST users/changepassword`
- Delete user at `DELETE users`
