rm *.sln
dotnet new sln
find "src" -name "*proj" | xargs -I {} dotnet sln add {}
find "tests" -name "*proj" | xargs -I {} dotnet sln add {}