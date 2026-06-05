param(
    [Parameter(Mandatory = $true)]
    [string] $DockerHubUser,

    [string] $Repository = "sideseat",
    [string] $Tag = "v0.1"
)

$image = "$DockerHubUser/$Repository`:$Tag"

docker build -f src/SideSeat/Dockerfile -t $image .
docker push $image

Write-Host "Pushed $image"
