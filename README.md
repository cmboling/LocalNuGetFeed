# LocalNuGetFeed
Private NuGet feed to store your packages

## Getting Started

0. Install .NET Core v.2.1+, Node.js v.10+ (if you haven't it)
1. Clone a current repository `https://github.com/beylkhanovdamir/LocalNuGetFeed.git`
2. Navigate to `.\LocalNuGetFeed\LocalNugetFeed\ClientApp`
3. Run the `npm install` command
4*. Run the `ng build --prod --aot` command
5. Move up on one level up to `.\LocalNuGetFeed\LocalNugetFeed\`
6. Start the service by `dotnet run` command
7. Open the URL http://localhost:5000/ in your browser

* If you're running a service on macOS and you don't have globally installed `angular/cli` package, then you should run the next command:

`$ alias ng="./localnugetfeed/localnugetfeed/clientapp/node_modules/@angular/cli/bin/ng"`

## How to push NuGet package

Run the below command:

`dotnet nuget push -s http://localhost:5000/v3/index.json {PackageFilePath}.nupkg` 

where *{PackageFilePath}* is the path to your NuGet package
