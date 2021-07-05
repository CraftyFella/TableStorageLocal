#!/bin/bash
set -e
rm -rf build
dotnet tool restore
dotnet tool list | cut -f1 -d " " | tail -n 2 | xargs -I {} dotnet tool update {}
if [ ! -d .paket ]; then
  dotnet paket install
else
  dotnet paket restore
fi
dotnet fantomas -r --check src &
dotnet fantomas -r --check test
pushd tests
dotnet run
popd