FROM microsoft/dotnet:2.0-runtime
COPY . /app
WORKDIR /app

WORKDIR /app
ENTRYPOINT dotnet run --configuration Release --project ./src/GelbooruChannelBot
