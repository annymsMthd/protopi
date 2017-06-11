FROM microsoft/dotnet

RUN mkdir /app
COPY . /app

WORKDIR /app

RUN dotnet restore

CMD dotnet run
