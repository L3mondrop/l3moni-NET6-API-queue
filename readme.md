Based on: https://docs.microsoft.com/en-us/learn/modules/build-web-api-minimal-api/3-exercise-create-minimal-api

dotnet new web -o l3moni-NET6-API-queue -f net6.0

dotnet add package Swashbuckle.AspNetCore --version 6.2.3
dotnet add package Azure.Storage.Queues --version 12.10.0