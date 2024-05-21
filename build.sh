rm -rf SmarterEnemies/bin
dotnet restore
dotnet build
rm -rf ~/.config/r2modmanPlus-local/RiskOfRain2/profiles/SmarterEnemies/BepInEx/plugins/SmarterEnemies
cp -r SmarterEnemies/bin/Debug/netstandard2.0  ~/.config/r2modmanPlus-local/RiskOfRain2/profiles/SmarterEnemies/BepInEx/plugins/SmarterEnemies

rm -rf SEBuild
mkdir SEBuild
cp icon.png SEBuild
cp manifest.json SEBuild
cp README.md SEBuild
cp SmarterEnemies/bin/Debug/netstandard2.0/SmarterEnemies.dll SEBuild
cd SEBuild
rm ../SE.zip
zip ../SE.zip *
cd ..
