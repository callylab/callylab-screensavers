param([string]$OutputDirectory="$PSScriptRoot/release")
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$csc="$env:WINDIR/Microsoft.NET/Framework64/v4.0.30319/csc.exe"
$refs=@('/reference:System.dll','/reference:System.Core.dll','/reference:System.Drawing.dll','/reference:System.Windows.Forms.dll')
$manifest="/win32manifest:$root\app.manifest"
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
foreach($name in @('ROME','EDO','HEARTH','NIGHTSHIFT')){
 $version=if($name -eq 'NIGHTSHIFT'){'1.0.1'}else{'1.1.0'}
 $dir=Join-Path $OutputDirectory "$name-$version"
 New-Item -ItemType Directory -Force $dir | Out-Null
 $symbol=if($name -eq 'HEARTH'){'VILLAGE'}else{$name}
 if($name -eq 'NIGHTSHIFT'){
  & $csc /nologo /target:winexe /optimize+ $manifest @refs "/resource:$root\city.png,city.png" "/resource:$root\nightshift.wav,nightshift.wav" "/out:$dir\$name.scr" "$root\Nightshift.cs"
 }else{
  & $csc /nologo /target:winexe /optimize+ $manifest @refs "/define:$symbol" "/resource:$root\$name.png,scene.png" "/out:$dir\$name.scr" "$root\HistoricalSaver.cs" "$root\HistoricalScene.cs"
 }
 if($LASTEXITCODE -ne 0){throw "$name compilation failed"}
 & $csc /nologo /target:winexe /optimize+ $manifest @refs "/define:$symbol" "/resource:$dir\$name.scr,payload.scr" "/out:$dir\$name-Setup.exe" "$root\Installer.cs"
 if($LASTEXITCODE -ne 0){throw "$name setup compilation failed"}
 Copy-Item "$root/INSTALL.txt" "$dir/INSTALL.txt"
 Get-ChildItem $dir -File | Where-Object Name -ne 'SHA256SUMS.txt' | ForEach-Object { (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLower()+'  '+$_.Name } | Set-Content "$dir/SHA256SUMS.txt"
 Compress-Archive -Path $dir -DestinationPath "$OutputDirectory/$name-$version.zip" -Force
}




