FROM mcr.microsoft.com/dotnet/core/sdk:3.1.402 AS build-env
ADD . /src
RUN dotnet publish /src/examples/fsharp/Azurite -c Release -o /app

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-alpine
WORKDIR /app
COPY --from=build-env /app .
EXPOSE 10002
ENTRYPOINT ["dotnet", "ConsoleApp.dll"]