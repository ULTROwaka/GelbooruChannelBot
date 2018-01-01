FROM microsoft/dotnet:2.0-runtime
RUN pwd
RUN ls
RUN env

COPY . /app
ARG source
RUN echo $source
WORKDIR /app
COPY ${source:-bin/Debug/netcoreapp2.0} .
#ENTRYPOINT ["dotnet", "GelbooruChannelBot.dll"]
ENTRYPOINT dotnet run --configuration Release --project ./src/GelbooruChannelBot