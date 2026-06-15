$privateKey = (Get-Content src/AuctionNest.API/keys/private.pem -Raw).Replace("`r`n", "\n").Replace("`n", "\n")
$publicKey  = (Get-Content src/AuctionNest.API/keys/public.pem  -Raw).Replace("`r`n", "\n").Replace("`n", "\n")

Write-Host "=== PRIVATE KEY ===" -ForegroundColor Yellow
Write-Host $privateKey
Write-Host ""
Write-Host "=== PUBLIC KEY ===" -ForegroundColor Cyan
Write-Host $publicKey