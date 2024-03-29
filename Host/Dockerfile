#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS vs-containerfastmode

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS builder
WORKDIR /src
COPY ["Host/Host.csproj", "Host/"]
COPY ["Service/Service.csproj", "Service/"]
COPY ["Contracts/Contracts.csproj", "Contracts/"]
RUN dotnet restore "Host/Host.csproj"

COPY . .
RUN dotnet build "Host/Host.csproj" \
    --configuration Release \
    --output /build \
    --no-restore

FROM builder AS publish
RUN dotnet publish "Host/Host.csproj" \
    --configuration Release \
    --output /output \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:3.1
ENV ASPNETCORE_URLS="http://0.0.0.0:8080"
WORKDIR /app
RUN mkdir ./configmap && chown 1000:1000 ./configmap
RUN mkdir ./secret && chown 1000:1000 ./secret
COPY --from=publish /output .
ENTRYPOINT ["dotnet", "pdf_generator.dll"]
