﻿param(
[int]$gameCode=0
)
# Initialize
$workpath = Split-Path -Parent $MyInvocation.MyCommand.Definition
cd $workpath

Write-Host "Ready Go!"

# Capture screenshot    
Write-Host "Captrue......"
sh cap.sh
Write-Host "Captrued"
$timeStamp = Get-Date
$timeStamp = $timeStamp.ToShortTimeString().Replace(':','-')
$filename = "screenshot$timeStamp.png"
copy screenshot.png $filename
D:\Workspace\AnswerApp\SearchEngine\SearchEngine\bin\Release\SearchEngine.exe "$workpath\$filename" "$gameCode"
Write-Host $LASTEXITCODE
# tap
adb shell input swipe 500 1700 500 1700 10