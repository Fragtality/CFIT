### PRE
### pwsh -ExecutionPolicy Unrestricted -file "..\NuUpdate.ps1" $(SolutionDir) $(TargetDir) "$(Configuration)" "PROJECTNAME" "CFIT.DEPENDENCY" ...

if ($args[0] -eq "*Undefined*") {
	exit 0
}

$basePath = $args[0]
$targetPath = $args[1]
$buildConfig = $args[2]
$packageName = $args[3]
$pathRepo = Join-Path $basePath "PackageRepo"

if (-not ($targetPath -match "net10")) {
	exit 0
}

$packCfg = "packages.config"
Function GetInstalledVersion{
	param ($package)
	if ((Test-Path -Path $packCfg)) {
		return (([xml](Get-Content $packCfg)).packages.ChildNodes | Where-Object id -like $package).version
	} else {
		$regex = (dotnet list package | Select-String -Pattern ($package + '\s+\S+\s+(\S+)'))
		if ($regex.Matches -and $regex.Matches.length -gt 0 -and $regex.Matches[0].Groups.length -gt 1) {
			return $regex.Matches[0].Groups[1].Value
		} else {
			return ""
		}
	}
}

Function UpdatePackage{
	param ($package)
	if ((Test-Path -Path $packCfg)) {
		Invoke-Expression "$nugetCli update $packCfg -Id $package -Source $pathRepo -NonInteractive -Verbosity quiet"
	} else {
		Invoke-Expression "dotnet add package $package" | Out-Null
	}
}

$projectDir = Join-Path $basePath $packageName
Write-Host "Checking NuGet Dependencies for CFIT.$packageName ..."
cd $projectDir
$count = 0
for ($index = 4; $index -lt $args.length; $index++) {
	$package = $args[$index]
	$packageVersion = GetInstalledVersion($package)
	$latestFile = (ls $pathRepo | Where-Object Name -like "$package*" | Sort-Object LastWriteTime)[-1].Name
	$latestVersion = (echo $latestFile | Select-String -Pattern '[^0-9]*(\d+\.\d+\.\d+\.\d+)\.nupkg').Matches[0].Groups[1].Value
	if ($latestVersion -and $packageVersion -and $latestVersion -ne $packageVersion) {
		Write-Host " => Updating '$package': $packageVersion => $latestVersion"
		UpdatePackage($package)
		$count++
	}	
}

if ($count -gt 0) {
	if (-not (Test-Path -Path $packCfg)) {
		dotnet restore --verbosity quiet
	}
}

exit 0