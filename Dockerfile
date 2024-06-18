FROM mcr.microsoft.com/dotnet/aspnet:8.0

EXPOSE 9006
ENV ASPNETCORE_URLS http://+:9006

WORKDIR /app
COPY "publish" .

ENTRYPOINT ["dotnet", "Api.dll"]