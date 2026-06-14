$code = @'
using System.Security.Cryptography;

var keysDir = "src/AuctionNest.API/keys";
Directory.CreateDirectory(keysDir);
var rsa = RSA.Create(2048);
File.WriteAllText($"{keysDir}/private.pem", rsa.ExportRSAPrivateKeyPem());
File.WriteAllText($"{keysDir}/public.pem", rsa.ExportSubjectPublicKeyInfoPem());
Console.WriteLine("RSA keys generated successfully!");
'@

$tmpDir = "_keygen_tmp"
dotnet new console -n KeyGen -o $tmpDir --no-restore 2>$null
Set-Content "$tmpDir/Program.cs" $code
dotnet run --project $tmpDir
Remove-Item -Recurse -Force $tmpDir