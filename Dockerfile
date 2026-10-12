FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT

WORKDIR /src

COPY ${PROJECT}/ ./

RUN dotnet restore
RUN dotnet publish -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
ARG PROJECT

WORKDIR /app

COPY --from=build /out ./

ENV APP_DLL=${PROJECT}.dll
ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

USER app

ENTRYPOINT ["sh", "-c", "exec dotnet \"$APP_DLL\""]