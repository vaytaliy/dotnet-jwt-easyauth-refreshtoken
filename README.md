# dotnet-jwt-easyauth-refreshtoken

This is a simple POC library for authentication using JWT
Works with MVC, API based applications
[Description in progress]

Requirements
- net5.0 or a later version
- implement ISimpleAuthDataAccess interface and add it to DI container, this allows middleware to fetch users from your storage
- Add a field to your storage to persist refresh tokens
- Add configuration to services used by the library
- that's all

Key features:
- Easy to set up and configure
- Database/storage agnostic (user defines access to storage adding own service implementation)
- Once access token expires, the app will check validity of refresh token and generate new pairs
- Tokens can be returned in cookies or response headers as configured by user
- Successfuly refreshed tokens allow to bypass "Authorize" attribute when original access token is expired
- When refresh token is expired, "Authorize" attribute prohibits access to requested resource
- Metainformation is sent in response headers: utc date of expiry for access and refresh token

Some things are still in the works and will be updated
