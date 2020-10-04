set -e
dotnet paket install
pushd tests
dotnet run
popd