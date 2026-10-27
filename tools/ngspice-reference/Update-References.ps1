[CmdletBinding()]
param(
    [string]$Configuration = 'Debug',
    [string[]]$Circuit = @()
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$circuitsRoot = Join-Path $repositoryRoot 'tests\reference-circuits'
$componentsDirectory = Join-Path $repositoryRoot 'game\content\components'
$cliProject = Join-Path $repositoryRoot 'src\Nodal.Cli\Nodal.Cli.csproj'
$cliAssembly = Join-Path $repositoryRoot "src\Nodal.Cli\bin\$Configuration\net10.0\Nodal.Cli.dll"
$operatingPointOptions = '.options reltol=1e-9 vntol=1e-12 abstol=1e-15 temp=27'
$transientOptions = '.options reltol=1e-6 vntol=1e-12 abstol=1e-15 chgtol=1e-16 trtol=1 method=trap temp=27'
$relativeToRange = 1e-4
$voltageFloor = 1e-6
$currentFloor = 1e-9
$pmosModelName = 'NODAL_PMOS'

function Get-NgspicePath {
    if (-not $env:NGSPICE) {
        throw 'The NGSPICE environment variable is not set.'
    }

    if (-not (Test-Path $env:NGSPICE)) {
        throw "NGSPICE points to $env:NGSPICE, which does not exist."
    }

    return $env:NGSPICE
}

function Get-NgspiceVersion([string]$ngspice) {
    $banner = & $ngspice -v
    $line = $banner | Where-Object { $_ -match 'ngspice-\d+' } | Select-Object -First 1

    if (-not $line) {
        throw 'Could not read the ngspice version banner.'
    }

    return [regex]::Match($line, 'ngspice-\d+(\.\d+)*').Value
}

function Get-CurrentAccessor([string]$name) {
    switch -Regex ($name.Substring(0, 1)) {
        '^[Rr]$' { return "@$name[i]" }
        '^[Cc]$' { return "@$name[i]" }
        '^[Ll]$' { return "@$name[i]" }
        '^[Dd]$' { return "@$name[id]" }
        '^[Ii]$' { return "@$name[current]" }
        '^[Vv]$' { return "$name#branch" }
        '^[Mm]$' { return "@$name[id]" }
        '^[Qq]$' { return "@$name[ic]" }
        default { throw "Unknown element type in $name." }
    }
}

function Get-CurrentSign([string]$name, [string[]]$deck) {
    if ($name.Substring(0, 1) -notmatch '^[Mm]$') {
        return 1
    }

    $pattern = '^\s*' + [regex]::Escape($name) + '\s'

    foreach ($line in $deck) {
        if (($line -match $pattern) -and ($line -match $pmosModelName)) {
            return -1
        }
    }

    return 1
}

function Get-ComponentSigns($component, [string[]]$deck) {
    $signs = New-Object System.Collections.Generic.List[int]

    foreach ($element in $component.elements) {
        $signs.Add((Get-CurrentSign $element $deck))
    }

    return $signs
}

function Get-NodeAccessors($probes) {
    return @($probes.nodes | Where-Object { $_ -ne '0' } | ForEach-Object { "v($_)" })
}

function Get-ElementAccessors($probes) {
    $accessors = New-Object System.Collections.Generic.List[string]

    foreach ($component in $probes.components) {
        foreach ($element in $component.elements) {
            $accessors.Add((Get-CurrentAccessor $element))
        }
    }

    return $accessors
}

function Get-DeckBody([string[]]$deck) {
    $lines = New-Object System.Collections.Generic.List[string]

    foreach ($line in $deck) {
        if ($line -match '^\s*\.end\s*$') {
            continue
        }

        $lines.Add($line)
    }

    return $lines
}

function New-OperatingPointDeck([string[]]$deck, [string[]]$accessors) {
    $lines = New-Object System.Collections.Generic.List[string]
    $lines.AddRange([string[]](Get-DeckBody $deck))

    $lines.Add($operatingPointOptions)
    $lines.Add('.control')
    $lines.Add('set numdgt=12')
    $lines.Add('op')

    foreach ($accessor in $accessors) {
        $lines.Add("print $accessor")
    }

    $lines.Add('.endc')
    $lines.Add('.end')

    return $lines
}

function New-TransientDeck([string[]]$deck, [string[]]$accessors, $transient, [string]$dataPath) {
    $lines = New-Object System.Collections.Generic.List[string]
    $lines.AddRange([string[]](Get-DeckBody $deck))

    $saved = $accessors -join ' '
    $stopTime = Format-Number $transient.stopTime
    $outputStep = Format-Number $transient.outputStep
    $maxStep = Format-Number $transient.maxStep
    $command = "tran $outputStep $stopTime 0 $maxStep"

    if ($transient.startFromZero) {
        $command = $command + ' uic'
    }

    $lines.Add($transientOptions)
    $lines.Add('.control')
    $lines.Add('set numdgt=12')
    $lines.Add("save $saved")
    $lines.Add($command)
    $lines.Add('linearize')
    $lines.Add("wrdata $($dataPath.Replace('\', '/')) $saved")
    $lines.Add('.endc')
    $lines.Add('.end')

    return $lines
}

function Invoke-Ngspice([string]$ngspice, [string]$deckPath) {
    $output = & $ngspice -b $deckPath 2>&1
    $text = $output -join "`n"

    if ($text -match '(?im)^\s*(error|fatal)') {
        throw "ngspice reported an error for $deckPath :`n$text"
    }

    return $output
}

function Read-PrintedValues([string[]]$output) {
    $values = @{}

    foreach ($line in $output) {
        $match = [regex]::Match($line, '^\s*(\S+)\s*=\s*([-+0-9.eE]+)\s*$')

        if ($match.Success) {
            $values[$match.Groups[1].Value.ToLowerInvariant()] =
                [double]::Parse($match.Groups[2].Value, [Globalization.CultureInfo]::InvariantCulture)
        }
    }

    return $values
}

function Read-Columns([string]$path, [int]$count) {
    $times = New-Object System.Collections.Generic.List[double]
    $series = New-Object System.Collections.Generic.List[System.Collections.Generic.List[double]]

    for ($index = 0; $index -lt $count; $index++) {
        $series.Add((New-Object System.Collections.Generic.List[double]))
    }

    foreach ($line in [IO.File]::ReadAllLines($path)) {
        $tokens = @(-split $line.Trim())

        if ($tokens.Count -lt 2 * $count) {
            continue
        }

        $times.Add([double]::Parse($tokens[0], [Globalization.CultureInfo]::InvariantCulture))

        for ($index = 0; $index -lt $count; $index++) {
            $series[$index].Add(
                [double]::Parse($tokens[2 * $index + 1], [Globalization.CultureInfo]::InvariantCulture))
        }
    }

    return [PSCustomObject]@{ Times = $times; Series = $series }
}

function Get-Value([hashtable]$values, [string]$key) {
    $lowered = $key.ToLowerInvariant()

    if (-not $values.ContainsKey($lowered)) {
        throw "ngspice did not print $key."
    }

    return $values[$lowered]
}

function Format-Number($value) {
    return ([double]$value).ToString('R', [Globalization.CultureInfo]::InvariantCulture)
}

function Format-Entries([System.Collections.Specialized.OrderedDictionary]$entries) {
    $formatted = New-Object System.Collections.Generic.List[string]
    $keys = @($entries.Keys)

    for ($index = 0; $index -lt $keys.Count; $index++) {
        $comma = ''

        if ($index -lt $keys.Count - 1) {
            $comma = ','
        }

        $value = Format-Number $entries[$keys[$index]]
        $formatted.Add("    `"$($keys[$index])`": $value$comma")
    }

    return $formatted
}

function Format-SeriesEntries([System.Collections.Specialized.OrderedDictionary]$entries) {
    $formatted = New-Object System.Collections.Generic.List[string]
    $keys = @($entries.Keys)

    for ($index = 0; $index -lt $keys.Count; $index++) {
        $comma = ''

        if ($index -lt $keys.Count - 1) {
            $comma = ','
        }

        $values = @($entries[$keys[$index]] | ForEach-Object { Format-Number $_ }) -join ', '
        $formatted.Add("    `"$($keys[$index])`": [$values]$comma")
    }

    return $formatted
}

function Write-ExpectedJson(
    [string]$path,
    [string]$version,
    [System.Collections.Specialized.OrderedDictionary]$nodeVoltages,
    [System.Collections.Specialized.OrderedDictionary]$elementCurrents) {

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add('{')
    $lines.Add('  "schemaVersion": 1,')
    $lines.Add("  `"generator`": `"$version`",")
    $lines.Add('  "nodeVoltages": {')
    $lines.AddRange([string[]](Format-Entries $nodeVoltages))
    $lines.Add('  },')
    $lines.Add('  "elementCurrents": {')
    $lines.AddRange([string[]](Format-Entries $elementCurrents))
    $lines.Add('  }')
    $lines.Add('}')

    [IO.File]::WriteAllText($path, ($lines -join "`n") + "`n")
}

function Write-ExpectedTransientJson(
    [string]$path,
    [string]$version,
    $transient,
    [System.Collections.Generic.List[double]]$times,
    [System.Collections.Specialized.OrderedDictionary]$nodeVoltages,
    [System.Collections.Specialized.OrderedDictionary]$elementCurrents) {

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add('{')
    $lines.Add('  "schemaVersion": 1,')
    $lines.Add("  `"generator`": `"$version`",")
    $lines.Add('  "transient": {')
    $lines.Add("    `"stopTime`": $(Format-Number $transient.stopTime),")
    $lines.Add("    `"outputStep`": $(Format-Number $transient.outputStep),")
    $lines.Add("    `"maxStep`": $(Format-Number $transient.maxStep),")
    $lines.Add("    `"startFromZero`": $($transient.startFromZero.ToString().ToLowerInvariant())")
    $lines.Add('  },')
    $lines.Add('  "tolerance": {')
    $lines.Add("    `"relativeToRange`": $(Format-Number $relativeToRange),")
    $lines.Add("    `"voltageFloor`": $(Format-Number $voltageFloor),")
    $lines.Add("    `"currentFloor`": $(Format-Number $currentFloor)")
    $lines.Add('  },')
    $lines.Add("  `"times`": [$((@($times | ForEach-Object { Format-Number $_ })) -join ', ')],")
    $lines.Add('  "nodeVoltages": {')
    $lines.AddRange([string[]](Format-SeriesEntries $nodeVoltages))
    $lines.Add('  },')
    $lines.Add('  "elementCurrents": {')
    $lines.AddRange([string[]](Format-SeriesEntries $elementCurrents))
    $lines.Add('  }')
    $lines.Add('}')

    [IO.File]::WriteAllText($path, ($lines -join "`n") + "`n")
}

function Update-OperatingPoint(
    [string]$ngspice,
    [string]$version,
    [string]$folder,
    [string[]]$deck,
    $probes,
    [string]$scratch,
    [string]$name) {

    $nodeAccessors = @(Get-NodeAccessors $probes)
    $elementAccessors = @(Get-ElementAccessors $probes)

    $runDeck = New-OperatingPointDeck $deck ($nodeAccessors + $elementAccessors)
    $runPath = Join-Path $scratch ($name + '-op.cir')
    [IO.File]::WriteAllText($runPath, ($runDeck -join "`n") + "`n")

    $values = Read-PrintedValues (Invoke-Ngspice $ngspice $runPath)

    $nodeVoltages = New-Object System.Collections.Specialized.OrderedDictionary

    foreach ($node in $probes.nodes) {
        $voltage = 0.0

        if ($node -ne '0') {
            $voltage = Get-Value $values "v($node)"
        }

        $nodeVoltages.Add($node, $voltage) | Out-Null
    }

    $elementCurrents = New-Object System.Collections.Specialized.OrderedDictionary

    foreach ($component in $probes.components) {
        $total = 0.0
        $signs = Get-ComponentSigns $component $deck

        for ($offset = 0; $offset -lt $component.elements.Count; $offset++) {
            $total = $total + $signs[$offset] *
                (Get-Value $values (Get-CurrentAccessor $component.elements[$offset]))
        }

        $elementCurrents.Add($component.designator, $total) | Out-Null
    }

    Write-ExpectedJson (Join-Path $folder 'expected.json') $version $nodeVoltages $elementCurrents
}

function Update-Transient(
    [string]$ngspice,
    [string]$version,
    [string]$folder,
    [string[]]$deck,
    $probes,
    [string]$scratch,
    [string]$name) {

    $nodeAccessors = @(Get-NodeAccessors $probes)
    $elementAccessors = @(Get-ElementAccessors $probes)
    $accessors = @($nodeAccessors + $elementAccessors)

    $dataPath = Join-Path $scratch ($name + '-tran.txt')
    $runDeck = New-TransientDeck $deck $accessors $probes.transient $dataPath
    $runPath = Join-Path $scratch ($name + '-tran.cir')
    [IO.File]::WriteAllText($runPath, ($runDeck -join "`n") + "`n")

    Invoke-Ngspice $ngspice $runPath | Out-Null

    if (-not (Test-Path $dataPath)) {
        throw "ngspice wrote no transient data for $name."
    }

    $columns = Read-Columns $dataPath $accessors.Count

    if ($probes.transient.startFromZero) {
        $columns.Times.RemoveAt(0)

        foreach ($column in $columns.Series) {
            $column.RemoveAt(0)
        }
    }

    $nodeVoltages = New-Object System.Collections.Specialized.OrderedDictionary
    $index = 0

    foreach ($node in $probes.nodes) {
        if ($node -eq '0') {
            continue
        }

        $nodeVoltages.Add($node, $columns.Series[$index]) | Out-Null
        $index++
    }

    $elementCurrents = New-Object System.Collections.Specialized.OrderedDictionary

    foreach ($component in $probes.components) {
        $total = New-Object System.Collections.Generic.List[double]
        $signs = Get-ComponentSigns $component $deck

        for ($row = 0; $row -lt $columns.Times.Count; $row++) {
            $sum = 0.0

            for ($offset = 0; $offset -lt $component.elements.Count; $offset++) {
                $sum = $sum + $signs[$offset] * $columns.Series[$index + $offset][$row]
            }

            $total.Add($sum)
        }

        $index = $index + $component.elements.Count
        $elementCurrents.Add($component.designator, $total) | Out-Null
    }

    Write-ExpectedTransientJson `
        (Join-Path $folder 'expected-transient.json') `
        $version `
        $probes.transient `
        $columns.Times `
        $nodeVoltages `
        $elementCurrents
}

$ngspice = Get-NgspicePath
$version = Get-NgspiceVersion $ngspice

Write-Host "Using $ngspice ($version)"

& dotnet build $cliProject -c $Configuration -v q --nologo | Out-Null

if ($LASTEXITCODE -ne 0) {
    throw 'Building Nodal.Cli failed.'
}

$folders = Get-ChildItem -Path $circuitsRoot -Directory |
    Where-Object { Test-Path (Join-Path $_.FullName 'circuit.nodal') }

if ($Circuit.Count -gt 0) {
    $folders = $folders | Where-Object { $Circuit -contains $_.Name }
}

$scratch = Join-Path ([IO.Path]::GetTempPath()) ('nodal-ngspice-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $scratch | Out-Null

try {
    foreach ($folder in $folders) {
        $schematicPath = Join-Path $folder.FullName 'circuit.nodal'
        $deckPath = Join-Path $folder.FullName 'circuit.cir'
        $probesPath = Join-Path $folder.FullName 'probes.json'

        & dotnet $cliAssembly export $schematicPath $deckPath $componentsDirectory | Out-Null

        if ($LASTEXITCODE -ne 0) {
            throw "Exporting $schematicPath failed."
        }

        $deck = [IO.File]::ReadAllLines($deckPath)
        $probes = Get-Content -Raw $probesPath | ConvertFrom-Json

        Update-OperatingPoint $ngspice $version $folder.FullName $deck $probes $scratch $folder.Name

        if ($probes.PSObject.Properties.Name -contains 'transient') {
            Update-Transient $ngspice $version $folder.FullName $deck $probes $scratch $folder.Name
            Write-Host "Updated $($folder.Name) (operating point and transient)"
        } else {
            Write-Host "Updated $($folder.Name) (operating point)"
        }
    }
} finally {
    Remove-Item -Recurse -Force $scratch -ErrorAction SilentlyContinue
}
