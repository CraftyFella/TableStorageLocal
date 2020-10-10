set -e
dotnet tool restore
dotnet paket install
pushd tests
dotnet run
popd