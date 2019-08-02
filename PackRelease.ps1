Param ( $version, $ow )

if (!$version) {
	$version = Read-Host 'Enter version tag for this release'
}

if (!(Test-Path 'EveWindowManager\bin\release\' -PathType Container)) { 
    write-host "Could not find the release build folder"
	exit
}

$outZipFile = "EveWindowManager-$version.zip"

if (Test-Path -path $outZipFile) {
	if (!$ow) {
		$ow = Read-Host 'There is already a release file with that version. Delete it and create new? (yes/no)'
	}

	if ($ow -eq 'yes') {
		write-host "Removing old zip file"
		Remove-Item -path $outZipFile
	} else {
		exit
	}
}

write-host "Creating new zip file"
Compress-Archive -Path `
	EveWindowManager\bin\release\*.exe, `
	EveWindowManager\bin\release\*.exe.config, `
	EveWindowManager\bin\release\*.dll `
	-CompressionLevel Optimal -DestinationPath $outZipFile

write-host "Done"