$source = $args[0]

Set-Location $PSScriptRoot\packages

Dir |
Where-Object { $_.Name -match "$source.[\d\.]+.nupkg" -and $_.Name -notmatch "\.(symbols)"} |
Remove-Item

Dir |
Where-Object { $_.Name.Contains($source) } |
Rename-Item -NewName { $_.Name -replace ".symbols","" }