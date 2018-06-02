properties { 
	$BaseDirectory = Resolve-Path .. 
    
    $ProjectName = "EffectiveTddDemo"
    
	$SrcDir = "$BaseDirectory\src"
    $ArtifactsDirectory = "$BaseDirectory\Artifacts"
    $SolutionFilePath = "$BaseDirectory\$ProjectName.sln"

    $NugetExe = "$BaseDirectory\lib\nuget.exe"
}

task default -depends DetermineMsBuildPath, RestoreNugetPackages,  Compile, RunTests

task RestoreNugetPackages {
    $packageConfigs = Get-ChildItem "$BaseDirectory" -Recurse | where{$_.Name -eq "packages.config"}

    foreach($packageConfig in $packageConfigs){
    	Write-Host "Restoring" $packageConfig.FullName 
    	exec { 
            . "$NugetExe" install $packageConfig.FullName -OutputDirectory "$BaseDirectory\packages" -NonInteractive
        }
    }
}

task DetermineMsBuildPath -depends RestoreNugetPackages {
	Write-Host "Adding msbuild to the environment path"

	$installationPath = & "$BaseDirectory\Packages\vswhere.2.4.1\tools\vswhere.exe" -latest -products * -requires Microsoft.Component.MSBuild -property installationPath

	if ($installationPath) {
		$msbuildPath = join-path $installationPath 'MSBuild\15.0\Bin'

		if (test-path $msbuildPath) {
                        Write-Host "msbuild directory set to $msbuildPath"
			$env:path = "$msbuildPath;$env:path"
		}
	}
}

task Compile -Description "Compiling solution." { 
	exec { msbuild /nologo /verbosity:minimal $SolutionFilePath /p:Configuration=Release }
}

task RunTests -depends Compile -Description "Running all unit tests." {
	$xunitRunner = "$BaseDirectory\packages\xunit.runner.console.2.3.1\tools\net452\xunit.console.exe"
    
    if (!(Test-Path $ArtifactsDirectory)) {
		New-Item $ArtifactsDirectory -Type Directory
	}

	exec { . $xunitRunner `
        "$BaseDirectory\Tests\ExampleHost.Specs\bin\Release\ExampleHost.Specs.dll" `
        -html "$ArtifactsDirectory\xunit.html" -parallel assemblies }
}

