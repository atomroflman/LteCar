FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG GIT_BRANCH=master
ARG GIT_COMMIT=unknown
WORKDIR /src

COPY Server/LteCar.Server.csproj Server/
COPY Shared/LteCar.Shared.csproj Shared/
RUN dotnet restore Server/LteCar.Server.csproj

COPY Server/ Server/
COPY Shared/ Shared/
RUN dotnet publish Server/LteCar.Server.csproj -c Release -o /app/publish /p:UseAppHost=false \
    /p:BuildInfoBranch="$GIT_BRANCH" /p:BuildInfoCommit="$GIT_COMMIT"

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:5000
EXPOSE 5000
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LteCar.Server.dll"]
