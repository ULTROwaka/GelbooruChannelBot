FROM microsoft/dotnet:2.0-runtime
RUN pwd
RUN ls ./var/lib/docker
RUN ls ./var/lib/docker/tmp
RUN ls ./var/lib/docker/tmp/docker-builder767315267/
RUN env

COPY . /app
ARG source
RUN echo $source
WORKDIR /app
COPY ${source:-GelbooruChannelBot/bin/Debug/netcoreapp2.0} .
ENTRYPOINT ["dotnet", "GelbooruChannelBot.dll