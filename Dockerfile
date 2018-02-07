FROM microsoft/dotnet:latest

COPY . /app
RUN bash -c 'ls -lah /app'

WORKDIR /app


#RUN dotnet restore GelbooruChannelBot.sln

#ENTRYPOINT dotnet run --configuration Debug --project ./GelbooruChannelBot
ENTRYPOINT ["dotnet", "GelbooruChannelBot.dll"]