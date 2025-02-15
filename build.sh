dotnet run --project ./CakeBuild/CakeBuild.csproj -- "$@"
rm -rf "$VINTAGE_STORY/Mods/serverauth"
cp -r ./Releases/serverauth "$VINTAGE_STORY/Mods/serverauth"