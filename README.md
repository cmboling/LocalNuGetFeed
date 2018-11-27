# LocalNuGetFeed
Private NuGet feed to store your packages

## Getting Started

0. Install **.NET Core v.2.1+, Node.js v.8.9+** (if you haven't it). *See notes below for more information*
1. Clone a current repository `https://github.com/beylkhanovdamir/LocalNuGetFeed.git`
2. Navigate to `.\LocalNuGetFeed\LocalNugetFeed\ClientApp`
3. Run the `npm install` command
4. Run the `ng build --prod --aot` command. *See notes below if you're using **macOS** before running `ng build` command*
5. Move up on one level up to `.\LocalNuGetFeed\LocalNugetFeed\`
6. Build the service project by `dotnet build` command
7. Start the service by `dotnet run --environment=Production` command
8. Open the URL http://localhost:5000/ in your browser

*NOTES:*

* Check the version of **npm** which should be **5.5.1** or higher, also you can upgrade it using the following
command: 

`npm install -g npm`

* If you're running a service on **macOS** and you don't have globally installed `angular/cli` package, then you should run the next command:

`$ alias ng="./localnugetfeed/localnugetfeed/clientapp/node_modules/@angular/cli/bin/ng"`

## How to push NuGet package

Run the below command:

`dotnet nuget push -s http://localhost:5000/v3/index.json {PackageFilePath}.nupkg` 

where *{PackageFilePath}* is the path to your NuGet package

## Running service on Docker
You can run this service on Docker.

0.Build the docker image by the following command:

`docker build -t localnugetfeed .`

Make sure that command was successfully completed to start use your tagged docker image *localnugetfeed:latest*

1. Run the `docker run --rm -it --name localnugetfeed -p 5555:80 localnugetfeed:latest` command

2. Push nuget package using the next command: `dotnet nuget push -s http://localhost:5555/v3/index.json {PackageFilePath}.nupkg`

3. Open the URL http://localhost:5555/ in your browser and try to search for your packages
