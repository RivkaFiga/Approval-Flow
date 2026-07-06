ARG DOTNET_SDK_VERSION=8.0
ARG DOTNET_ASPNET_VERSION=8.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION} AS build
ARG PROJECT_PATH
ARG PROJECT_NAME
WORKDIR /src

COPY Directory.Build.props ./
COPY ApprovalFlow.sln ./
COPY src ./src

RUN dotnet restore "${PROJECT_PATH}/${PROJECT_NAME}.csproj"
RUN dotnet publish "${PROJECT_PATH}/${PROJECT_NAME}.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_ASPNET_VERSION} AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ARG PROJECT_NAME
ENV APP_DLL=${PROJECT_NAME}.dll
ENTRYPOINT ["/bin/sh","-c","exec dotnet ${APP_DLL}"]
