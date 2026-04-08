param(
	[Parameter(Mandatory)][string] $Source,
	[Parameter(Mandatory)][string] $Out,
	[int] $Quality = 100
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Add-Type -AssemblyName System.Drawing

$image =[System.Drawing.Image]::FromFile($Source)
try {
	$encoder = [System.Drawing.Imaging.Encoder]::Quality
	$encoderParams = New-Object System.Drawing.Imaging.EncoderParameters(1)
	$encoderParams.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter($encoder, $Quality)

	$jpegCodec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object { $_.MimeType -eq 'image/jpeg' }

	$image.Save($Out, $jpegCodec, $encoderParams)

} finally {
	$image.Dispose()
}
