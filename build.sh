set -e
dotnet tool restore
dotnet paket install
pushd examples/ConsoleApp
dotnet run
popd
pushd tests
dotnet run
popd