FROM microsoft/dotnet:2.0-sdk-jessie

COPY . /app
WORKDIR /app
RUN dotnet restore

WORKDIR /app

ENTRYPOINT dotnet run --configuration Release --project ./GelbooruChannelBot
