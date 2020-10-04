NUGETVERSION=0.0.1
dotnet pack src/AzureTableStorage/AzureTableStorage.fsproj -c Release /p:PackageVersion=$NUGETVERSION
# dotnet nuget push AzureTableStorage/bin/Release/AzureTableStorage.$NUGETVERSION.nupkg -k $NUGETKEY -s nuget.org