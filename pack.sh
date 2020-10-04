NUGETVERSION=$1
dotnet pack src/FakeAzureTables/FakeAzureTables.fsproj -c Release /p:PackageVersion=$NUGETVERSION
dotnet nuget push src/FakeAzureTables/bin/Release/FakeAzureTables.$NUGETVERSION.nupkg -k $NUGETKEY -s nuget.org