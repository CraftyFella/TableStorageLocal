set -e
dotnet paket install
pushd examples/ConsoleApp
dotnet run
popd
pushd tests
dotnet run
popd