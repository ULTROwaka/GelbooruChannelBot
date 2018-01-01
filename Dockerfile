FROM microsoft/dotnet:2.0.0-sdk
RUN bash -c 'echo -e START'
COPY . /app
RUN bash -c 'echo -e /app ls'

RUN bash -c 'ls -la /app'
WORKDIR /app
RUN dotnet restore GelbooruChannelBot.sln

WORKDIR /app

ENTRYPOINT dotnet run --configuration Release --project ./GelbooruChannelBot
