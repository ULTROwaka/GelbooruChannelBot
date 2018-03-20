FROM microsoft/dotnet:latest

COPY . /app
RUN bash -c 'ls -la /app'
RUN add-apt-repository ppa:mc3man/trusty-media
RUN apt-get update
RUN apt-get dist-upgrade
RUN apt-get install ffmpeg -y
WORKDIR /app
#RUN dotnet restore GelbooruChannelBot.sln

ENTRYPOINT dotnet run --configuration Release --project ./GelbooruChannelBot
