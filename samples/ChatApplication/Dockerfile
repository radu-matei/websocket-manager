FROM microsoft/dotnet:1.1.0-runtime

WORKDIR /app

COPY . .

EXPOSE 8080

EXPOSE 5000/tcp
ENV ASPNETCORE_URLS http://*:5000

ENTRYPOINT ["dotnet", "bin/Debug/netcoreapp1.1/publish/ChatApplication.dll"]
