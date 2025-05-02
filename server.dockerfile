# Use the official .NET SDK image as a base image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory in the container
WORKDIR /app


# Copy the rest of the application files into the container
COPY . .

RUN /bin/bash /app/pi-install-server.sh
# RUN chmod +x pi-install-server.sh

# RUN /bin/bash /app/pi-install-server.sh

# Expose the port the application runs on
# http api janus | janus-gateway ws | server
EXPOSE 8088 8188 5000

# Expose RTP/RTCP-Portbereich
EXPOSE 10000-10200/udp

# Run the application
CMD ["dotnet", "run"]