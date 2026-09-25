# One Dockerfile for every .NET host in the solution:
#   docker build -f deploy/Dockerfile --build-arg PROJECT=src/Services/Shipping/Deliver.Shipping.Api --build-arg ASSEMBLY=Deliver.Shipping.Api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT
WORKDIR /repo
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/ src/
RUN dotnet publish "$PROJECT" -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG ASSEMBLY
ENV APP_ASSEMBLY=${ASSEMBLY} \
    ASPNETCORE_HTTP_PORTS=8080
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec dotnet \"$APP_ASSEMBLY.dll\""]
