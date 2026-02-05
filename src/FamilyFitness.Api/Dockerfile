FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and projects
COPY FamilyFitness.sln .
COPY src/FamilyFitness.Domain/*.csproj ./src/FamilyFitness.Domain/
COPY src/FamilyFitness.Application/*.csproj ./src/FamilyFitness.Application/
COPY src/FamilyFitness.Infrastructure/*.csproj ./src/FamilyFitness.Infrastructure/
COPY src/FamilyFitness.Api/*.csproj ./src/FamilyFitness.Api/

# Restore dependencies
RUN dotnet restore src/FamilyFitness.Api/FamilyFitness.Api.csproj

# Copy source code
COPY src/ ./src/

# Build and publish
WORKDIR /src/src/FamilyFitness.Api
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "FamilyFitness.Api.dll"]
