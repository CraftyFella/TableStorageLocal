NUGETVERSION=0.0.1-alpha
dotnet pack src/AzureTableStorage/AzureTableStorage.fsproj -c Release /p:PackageVersion=$NUGETVERSION
dotnet nuget push src/AzureTableStorage/bin/Release/FakeAzureTables.$NUGETVERSION.nupkg -k $NUGETKEY -s nuget.org