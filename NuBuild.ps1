### POST
### pwsh -ExecutionPolicy Unrestricted -file "..\NuBuild.ps1" $(SolutionDir) $(TargetDir) "$(Configuration)" "PROJECTNAME" 

if ($args[0] -eq "*Undefined*") {
	exit 0
}

$basePath = $args[0]
$targetPath = $args[1]
$buildConfig = $args[2]
$packageName = $args[3]

if (-not ($targetPath -match "net10")) {
	exit 0
}
$packageRepo = Join-Path $basePath "PackageRepo"
$projectDir = Join-Path $basePath $packageName
$outDir = Join-Path $basePath ".build"
$packageName = "CFIT." + $packageName
$major = [int]((Get-Date).Year)
$minor = [int]((Get-Date).DayOfYear.ToString("000"))
$build = [int](Get-Date -Format "HH")
$revision = [int](([int](Get-Date -Format "mm") * 60) + [int](Get-Date -Format "ss"))
$packageVersion = "$major.$minor.$build.$revision"
$packageId = "$packageName.$packageVersion"

cd $projectDir
Write-Host ("Build NuGet Package '$packageId' ...")
Remove-Item -Path ($outDir + "\*") -Force -ErrorAction SilentlyContinue | Out-Null
Invoke-Expression "dotnet pack --configuration $buildConfig --include-symbols --output $outDir -p:Version='$major.$minor.$build.$revision' --runtime win-x64 --verbosity quiet"

$outFile = Join-Path $outDir "$packageId.nupkg"	
Write-Host ("Add NuGet Package to local CFIT Repository ...")
Invoke-Expression "dotnet nuget push $outFile -s $packageRepo" | Out-Null
$outFile = Join-Path $outDir "$packageId.symbols.nupkg"
Invoke-Expression "dotnet nuget push $outFile -s $packageRepo" | Out-Null


exit 0