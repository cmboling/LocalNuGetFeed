# LocalNuGetFeed
Private NuGet feed to store your packages

## Getting Started

0. Install .NET Core v.2.1+ (if you haven't it)
1. Clone a current repository `https://github.com/beylkhanovdamir/LocalNuGetFeed.git`
2. Navigate to `.\LocalNuGetFeed\LocalNugetFeed\ClientApp`
3. Run the `npm install` command
4. Move up on one level up to `.\LocalNuGetFeed\LocalNugetFeed\`
5. Start the service by `dotnet run` command
6. Open the URL http://localhost:5000/ in your browser

## How to push NuGet package

Run the below command:

`dotnet nuget push -s http://localhost:5000/v3/index.json {PackageFilePath}.nupkg` 

where *{PackageFilePath}* is the path to your NuGet package
