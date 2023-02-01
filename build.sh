rm -rf SmarterEnemies/bin
dotnet restore
dotnet build
rm -rf ~/.config/r2modmanPlus-local/RiskOfRain2/profiles/SmarterEnemies/BepInEx/plugins/SmarterEnemies
cp -r SmarterEnemies/bin/Debug/netstandard2.0  ~/.config/r2modmanPlus-local/RiskOfRain2/profiles/SmarterEnemies/BepInEx/plugins/SmarterEnemies
