# Fix missing final newline in .cs files (does not commit changes)
$files = Get-ChildItem -Recurse -Filter *.cs
foreach ($f in $files) {
    $path = $f.FullName
    try {
        $bytes = [System.IO.File]::ReadAllBytes($path)
    } catch {
        Write-Output "Skip (read error): $path"
        continue
    }

    if ($bytes.Length -eq 0) { continue }

    if ($bytes[$bytes.Length - 1] -ne 10) {
        try {
            $s = [System.IO.File]::ReadAllText($path)
            $s = [System.Text.RegularExpressions.Regex]::Replace($s, "(\r?\n)*\z", "`r`n")
            [System.IO.File]::WriteAllText($path, $s)
            Write-Output "Fixed: $path"
        } catch {
            Write-Output "Skip (write error): $path"
        }
    }
}
Write-Output "Done"
