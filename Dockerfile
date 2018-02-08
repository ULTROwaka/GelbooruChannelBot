FROM microsoft/dotnet:latest

COPY . /app
RUN bash -c 'ls -la /app'
WORKDIR /app
#RUN dotnet restore GelbooruChannelBot.sln

ENTRYPOINT dotnet run --configuration Debug --project ./GelbooruChannelBot
