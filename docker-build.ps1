param (
    [string]$tag = "latest"
)

docker build . -f src\Spard.Service\Dockerfile -t vladimirkhil/spard:$tag