param (
    [string]$Token = $env:GITHUB_PAT,
    [switch]$NoPush = $false
)

$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$versionFile = Join-Path $root "version.txt"
$csprojFile = Join-Path $root "XboxKit.GUI\XboxKit.GUI.csproj"
$screenshotPath = Join-Path $root "docs\screenshot.png"
$publishExe = Join-Path $root "publish\XboxKit-GUI.exe"

# 1. 버전 읽기
if (-not (Test-Path $versionFile)) {
    "1.0.0" | Out-File -FilePath $versionFile -Encoding utf8
}
$currentVerStr = (Get-Content $versionFile -Raw).Trim()
if ([string]::IsNullOrWhiteSpace($currentVerStr)) {
    $currentVerStr = "1.0.0"
}

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  XboxKit GUI 빌드 시작: 버전 $currentVerStr" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

# 2. csproj 파일 버전 갱신
$csprojContent = Get-Content $csprojFile -Raw
$csprojContent = [regex]::Replace($csprojContent, "<Version>[^<]+</Version>", "<Version>$currentVerStr</Version>")
$csprojContent = [regex]::Replace($csprojContent, "<AssemblyVersion>[^<]+</AssemblyVersion>", "<AssemblyVersion>$currentVerStr</AssemblyVersion>")
$csprojContent = [regex]::Replace($csprojContent, "<FileVersion>[^<]+</FileVersion>", "<FileVersion>$currentVerStr</FileVersion>")
$csprojContent | Set-Content $csprojFile -Encoding utf8

# 3. README.md 파일 버전 및 빌드 정보 갱신
$readmeFile = Join-Path $root "README.md"
if (Test-Path $readmeFile) {
    $readmeContent = Get-Content $readmeFile -Raw
    $readmeContent = [regex]::Replace($readmeContent, "badge/Version-v[^-\)]+-brightgreen\.svg", "badge/Version-v$currentVerStr-brightgreen.svg")
    $readmeContent | Set-Content $readmeFile -Encoding utf8
}

# 4. 실행 중인 기존 프로세스 종료
Get-Process -Name "XboxKit-GUI" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

# 4. 단일 실행 파일 퍼블리시
Write-Host "[1/4] .NET 8.0 단일 exe 퍼블리시 빌드 중..." -ForegroundColor Yellow
$dotnet = "$env:LocalAppData\Microsoft\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    $dotnet = "dotnet"
}

& $dotnet publish (Join-Path $root "XboxKit.GUI\XboxKit.GUI.csproj") -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:EnableCompressionInSingleFile=true -o (Join-Path $root "publish")
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish 실패 (종료 코드: $LASTEXITCODE)"
}

# 5. 최신 UI 스크린샷 갱신
Write-Host "[2/4] 최신 스크린샷 캡처 중..." -ForegroundColor Yellow
Start-Process -FilePath $publishExe -ArgumentList "--screenshot", $screenshotPath -Wait

# 6. 다음 빌드를 위한 버전 0.0.1 증가
$parts = $currentVerStr.Split('.')
$major = [int]$parts[0]
$minor = [int]$parts[1]
$patch = [int]$parts[2]

$nextPatch = $patch + 1
$nextVerStr = "$major.$minor.$nextPatch"
$nextVerStr | Set-Content $versionFile -Encoding utf8
Write-Host "버전 카운터 갱신: $currentVerStr -> 다음 빌드 예정: $nextVerStr" -ForegroundColor Green

# 7. Git 커밋 및 Push
if (-not $NoPush) {
    Write-Host "[3/4] Git 변경사항 커밋 중..." -ForegroundColor Yellow
    Set-Location $root
    git add -A
    git commit -m "Build v${currentVerStr}: Update title bar build version and screenshot"
    
    if (-not [string]::IsNullOrEmpty($Token)) {
        Write-Host "[4/4] GitHub 원격 저장소에 Push 중..." -ForegroundColor Yellow
        git push "https://${Token}@github.com/RR-korea/XboxKit.git" main
    } else {
        Write-Host "[4/4] Token 미제공: git push origin main 실행..." -ForegroundColor Yellow
        git push origin main
    }
}

Write-Host "==================================================" -ForegroundColor Green
Write-Host "  빌드 v$currentVerStr 완료!" -ForegroundColor Green
Write-Host "  실행 파일: $publishExe" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green
