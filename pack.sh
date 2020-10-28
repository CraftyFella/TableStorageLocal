NUGETVERSION=$1
dotnet pack src/TableStorageLocal/TableStorageLocal.fsproj -c Release /p:PackageVersion=$NUGETVERSION
dotnet nuget push src/TableStorageLocal/bin/Release/TableStorageLocal.$NUGETVERSION.nupkg -k $NUGETKEY -s nuget.org