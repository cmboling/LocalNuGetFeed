FROM microsoft/dotnet:2.1-sdk as build-env
WORKDIR /app

# Setup nodejs/npm and anglar/cli
ENV NODE_VERSION 8.9.4
ENV NODE_DOWNLOAD_SHA 21fb4690e349f82d708ae766def01d7fec1b085ce1f5ab30d9bda8ee126ca8fc
RUN curl -SL "https://nodejs.org/dist/v${NODE_VERSION}/node-v${NODE_VERSION}-linux-x64.tar.gz" --output nodejs.tar.gz \
    && echo "$NODE_DOWNLOAD_SHA nodejs.tar.gz" | sha256sum -c - \
    && tar -xzf "nodejs.tar.gz" -C /usr/local --strip-components=1 \
    && rm nodejs.tar.gz \
    && ln -s /usr/local/bin/node /usr/local/bin/nodejs \
    && npm i -g @angular/cli npm i -g @angular/cli

# Copy web and core projects
COPY /LocalNugetFeed ./LocalNugetFeed

COPY /LocalNugetFeed.Core ./LocalNugetFeed.Core

# restore npm packages and install them
RUN cd LocalNugetFeed/ClientApp \
	&& npm install \
	&& ng build --prod --aot 

# restore nuget packages dependencies 
RUN cd LocalNugetFeed \
    && dotnet restore 

# publish app	
RUN cd LocalNugetFeed \
    && dotnet publish -c Release -o /app/out

# Build runtime image
FROM microsoft/dotnet:2.1-aspnetcore-runtime
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "LocalNugetFeed.Web.dll"]