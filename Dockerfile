FROM microsoft/dotnet:2.0-sdk-jessie

COPY . /app
RUN bash -c 'ls -la /app'
WORKDIR /app
#RUN dotnet restore GelbooruChannelBot.sln

ENTRYPOINT dotnet run --configuration Release --project ./GelbooruChannelBot
