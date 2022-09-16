app_name="vineyard_deploy"
app_description="vineyard_deploy"
user_name=$USER
dotnet_path=$(which dotnet)
working_directory=$(pwd)/srv
exec_absolute_path=$working_directory/$app_name.dll

cp -r ../vineyard_map/ ./
dotnet publish -c Release -o ./srv
p=$(pwd)/$app_name.dll
cat WorkerApp.service.tmplt |
sed "s|{app_name}|"$app_name"|" |
sed "s|{app_description}|"$app_description"|" |
sed "s|{user_name}|"$user_name"|" |
sed "s|{dotnet_path}|"$dotnet_path"|" |
sed "s|{working_directory}|"$working_directory"|" |
sed "s|{DOTNET_ROOT}|"$DOTNET_ROOT"|" |
sed "s|{exec_absolute_path}|"$exec_absolute_path"|" \
> $app_name.service
sudo rm -f /etc/systemd/system/$app_name.service
sudo ln ./$app_name.service /etc/systemd/system/$app_name.service
sudo systemctl daemon-reload
echo "service "$app_name" created"