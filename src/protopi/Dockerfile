FROM resin/rpi-raspbian

RUN apt-get update
RUN apt-get install -y libunwind8 libunwind8-dev gettext libicu-dev liblttng-ust-dev libcurl4-openssl-dev libssl-dev uuid-dev unzip

RUN mkdir /app
COPY . /app

RUN chmod +x /app/protopi
RUN chmod +x /app/libgrpc_csharp_ext.x86.so
WORKDIR /app

CMD ./protopi
