param([Parameter(Mandatory=$true)][string]$VideoPath)
$ErrorActionPreference='Stop'
if (!(Test-Path -LiteralPath $VideoPath -PathType Leaf)) { throw 'Video file not found' }
$audioRoot=Join-Path $PSScriptRoot 'local-audio'
New-Item -ItemType Directory -Force -Path $audioRoot | Out-Null
# Segments follow the supplied video's visible labels. Per the user's mapping,
# the opening headshot call (0-1.29) is used for the first kill.
$edges=@(0,1.29,2.656,4.123,5.556,6.956,8.723,10.623,12.886)
for($i=0;$i -lt 8;$i++) {
    $duration=$edges[$i+1]-$edges[$i]
    $start=$edges[$i].ToString('0.000',[Globalization.CultureInfo]::InvariantCulture)
    $length=$duration.ToString('0.000',[Globalization.CultureInfo]::InvariantCulture)
    $fade=($duration-.008).ToString('0.000',[Globalization.CultureInfo]::InvariantCulture)
    & ffmpeg -hide_banner -loglevel error -ss $start -i $VideoPath -t $length -vn -ac 1 -ar 22050 -c:a pcm_s16le -af "afade=t=in:d=0.004,afade=t=out:st=${fade}:d=0.008" -y (Join-Path $audioRoot ('streak-'+($i+1)+'.wav'))
    if($LASTEXITCODE -ne 0) { throw 'Audio extraction failed' }
}
Write-Output $audioRoot
