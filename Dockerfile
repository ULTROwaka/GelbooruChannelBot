FROM microsoft/dotnet:2.0-runtime
ARG source
WORKDIR /app
COPY ${source:-obin/Debug/netcoreapp2.0} .
ENTRYPOINT ["dotnet", "GelbooruChannelBot.dll"]
