param(
	[Parameter(Mandatory)][string] $Source,
	[Parameter(Mandatory)][string] $Out
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$helpSourcePath = Join-Path -Path $Source -ChildPath 'help.xhtml'

[xml]$doc = Get-Content -LiteralPath $helpSourcePath -Raw -Encoding UTF8

$ns = New-Object System.Xml.XmlNamespaceManager($doc.NameTable)
$ns.AddNamespace('h', 'help')

function Get-AttrValue {
	param(
		[Parameter(Mandatory)] [System.Xml.XmlElement] $Element,
		[Parameter(Mandatory)] [string] $Name
	)
	if (-not $Element.HasAttribute($Name)) { return '' }
	$v = $Element.GetAttribute($Name)
	if ($null -eq $v) { return '' }
	return $v.Trim()
}

function Get-LangText {
	param(
		[Parameter(Mandatory)] [System.Xml.XmlElement] $TextElement,
		[Parameter(Mandatory)] [string] $Lang
	)

	$candidates = @($Lang, 'en', 'jp')
	foreach ($candidate in $candidates) {
		$val = Get-AttrValue -Element $TextElement -Name $candidate
		if ($val -ne '') { return $val }
	}
	return ''
}

function Wrap-Style {
	param(
		[Parameter(Mandatory)] [string] $Text,
		[Parameter(Mandatory)] [System.Xml.XmlElement] $StyleElement
	)

	$result = $Text
	if ($result -eq '') { return '' }

	$italic = Get-AttrValue -Element $StyleElement -Name 'italic'
	$bold = Get-AttrValue -Element $StyleElement -Name 'bold'
	$color = Get-AttrValue -Element $StyleElement -Name 'color'
	$size = Get-AttrValue -Element $StyleElement -Name 'value'

	if ($italic -eq 'true') { $result = "<i>$result</i>" }
	if ($bold -eq 'true') { $result = "<b>$result</b>" }
	if ($color -ne '') { $result = "<color=$color>$result</color>" }
	if ($size -ne '') { $result = "<size=$size>$result</size>" }

	return $result
}

function Convert-InlineContainer {
	param(
		[Parameter(Mandatory)] [System.Xml.XmlElement] $Container,
		[Parameter(Mandatory)] [string] $Lang
	)

	$parts = New-Object System.Collections.Generic.List[string]
	foreach ($child in $Container.ChildNodes) {
		if ($child.NodeType -ne [System.Xml.XmlNodeType]::Element) { continue }

		$el = [System.Xml.XmlElement]$child
		$name = $el.LocalName

		if ($name -eq 'text') {
			$t = Get-LangText -TextElement $el -Lang $Lang
			if ($t -ne '') { [void]$parts.Add($t) }
			continue
		}

		if ($name -eq 'style') {
			foreach ($styleTextNode in $el.ChildNodes) {
				if ($styleTextNode.NodeType -ne [System.Xml.XmlNodeType]::Element) { continue }
				$styleTextEl = [System.Xml.XmlElement]$styleTextNode
				if ($styleTextEl.LocalName -ne 'text') { continue }
				$raw = Get-LangText -TextElement $styleTextEl -Lang $Lang
				$styled = Wrap-Style -Text $raw -StyleElement $el
				if ($styled -ne '') { [void]$parts.Add($styled) }
			}
		}
	}

	return ($parts -join '')
}

function Write-PageContent {
	param(
		[Parameter(Mandatory)] [System.Xml.XmlElement] $Page,
		[Parameter(Mandatory)] [string] $Lang,
		[System.Collections.Generic.List[string]] $Lines,
		[int] $PageIndex = 1
	)

	$titleNode = $Page.SelectSingleNode('./h:title/h:text', $ns)
	$titleText = ''
	if ($null -ne $titleNode) {
		$titleText = Get-LangText -TextElement ([System.Xml.XmlElement]$titleNode) -Lang $Lang
	}
	if ([string]::IsNullOrWhiteSpace($titleText)) {
		$titleText = "Page$PageIndex"
	}
	[void]$Lines.Add("`$$titleText")

	foreach ($child in $Page.ChildNodes) {
		if ($child.NodeType -ne [System.Xml.XmlNodeType]::Element) { continue }
		$el = [System.Xml.XmlElement]$child
		$name = $el.LocalName
		if ($name -eq 'title') { continue }

		switch ($name) {
			'topic' {
				$txt = Convert-InlineContainer -Container $el -Lang $Lang
				if ($txt -ne '') { [void]$Lines.Add(('{{topic|{0}}}' -f $txt)) }
				continue
			}
			'center' {
				[void]$Lines.Add('{center}')
				continue
			}
			'p' {
				$txt = Convert-InlineContainer -Container $el -Lang $Lang
				if ($txt -ne '') { [void]$Lines.Add($txt) }
				continue
			}
			'style' {
				$txt = Convert-InlineContainer -Container $el -Lang $Lang
				if ($txt -ne '') { [void]$Lines.Add($txt) }
				continue
			}
			'pair' {
				$pairChildren = @()
				foreach ($pairNode in $el.ChildNodes) {
					if ($pairNode.NodeType -eq [System.Xml.XmlNodeType]::Element) {
						$pairChildren += [System.Xml.XmlElement]$pairNode
					}
				}

				for ($i = 0; $i -lt $pairChildren.Count; $i += 2) {
					if ($i + 1 -ge $pairChildren.Count) { break }
					$keyEl = $pairChildren[$i]
					$valEl = $pairChildren[$i + 1]
					if ($keyEl.LocalName -ne 'key' -or $valEl.LocalName -ne 'value') { continue }
					$keyText = Convert-InlineContainer -Container $keyEl -Lang $Lang
					$valText = Convert-InlineContainer -Container $valEl -Lang $Lang
					[void]$Lines.Add(('{{pair|{0}|{1}}}' -f $keyText, $valText))
				}
				continue
			}
			'list' {
				foreach ($itemNode in $el.ChildNodes) {
					if ($itemNode.NodeType -ne [System.Xml.XmlNodeType]::Element) { continue }
					$itemEl = [System.Xml.XmlElement]$itemNode
					if ($itemEl.LocalName -ne 'item') { continue }
					$itemText = Convert-InlineContainer -Container $itemEl -Lang $Lang
					if ($itemText -ne '') { [void]$Lines.Add(('・ {0}' -f $itemText)) }
				}
				continue
			}
			'qa' {
				$qaChildren = @()
				foreach ($qaNode in $el.ChildNodes) {
					if ($qaNode.NodeType -eq [System.Xml.XmlNodeType]::Element) {
						$qaChildren += [System.Xml.XmlElement]$qaNode
					}
				}

				for ($i = 0; $i -lt $qaChildren.Count; $i += 2) {
					if ($i + 1 -ge $qaChildren.Count) { break }
					$qEl = $qaChildren[$i]
					$aEl = $qaChildren[$i + 1]
					if ($qEl.LocalName -ne 'q' -or $aEl.LocalName -ne 'a') { continue }
					$qText = Convert-InlineContainer -Container $qEl -Lang $Lang
					$aText = Convert-InlineContainer -Container $aEl -Lang $Lang
					[void]$Lines.Add(('{{Q|{0}}}' -f $qText))
					[void]$Lines.Add(('{{A|{0}}}' -f $aText))
				}
				continue
			}
			'link' {
				$label = Convert-InlineContainer -Container $el -Lang $Lang
				$href = Get-AttrValue -Element $el -Name 'href'
				[void]$Lines.Add(('{{link|{0}|{1}}}' -f $label, $href))
				continue
			}
			'image' {
				$src = Get-AttrValue -Element $el -Name 'src'
				if ($src -ne '') {
					$index = $src.LastIndexOf('.')
					if($index -ne -1) {
						$imagePath = $src.Substring(0, $index)
						[void]$Lines.Add(('{{image,{0}}}' -f $imagePath))
					}
				}
				continue
			}
			'br' {
				$value = Get-AttrValue -Element $el -Name 'value'
				if ($value -ne '') {
					[void]$Lines.Add((''))
				} else {
					[void]$Lines.Add('')
				}
				continue
			}
			Default {
				continue
			}
		}
	}

	[void]$Lines.Add('')
}

$pages = $doc.SelectNodes('//h:page', $ns)
if ($null -eq $pages -or $pages.Count -eq 0) {
	throw 'help:page が見つかりません。'
}

$langSet = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
$pages.SelectNodes('.//h:text', $ns) | ForEach-Object {
	$element = [System.Xml.XmlElement]$_
	foreach ($attr in $element.Attributes) {
		if ($attr.Prefix -eq 'xmlns' -or $attr.Name -eq 'xmlns') { continue }
		if ([string]::IsNullOrWhiteSpace($attr.LocalName)) { continue }
		[void]$langSet.Add($attr.LocalName.ToLowerInvariant())
	}
}

$langs = @($langSet)
if ($langs.Count -eq 0) {
	throw 'help:text の属性から言語コードを取得できませんでした。'
}

$langFolderMap = @{
	'jp'   = 'JP'
	'en'   = 'EN'
	'cn'   = 'CN'
	'zhtw' = 'ZHTW'
	'kr'   = 'KR'
}

foreach ($lang in $langs) {
	$lines = New-Object System.Collections.Generic.List[string]
	for ($pi = 0; $pi -lt $pages.Count; $pi++) {
		$pageNode = [System.Xml.XmlElement]$pages[$pi]
		Write-PageContent -Page $pageNode -Lang $lang -Lines $lines -PageIndex ($pi + 1)
	}

	$langFolder = $langFolderMap[$lang]
	if ([string]::IsNullOrWhiteSpace($langFolder)) { $langFolder = $lang.ToUpperInvariant() }

	$outPath = Join-Path -Path $Out -ChildPath (Join-Path -Path "LangMod\$langFolder" -ChildPath 'Text\Help\help.txt')
	$outDir = Split-Path -Path $outPath -Parent
	if (-not (Test-Path -LiteralPath $outDir)) {
		[void](New-Item -ItemType Directory -Path $outDir -Force)
	}

	[System.IO.File]::WriteAllLines($outPath, $lines, (New-Object System.Text.UTF8Encoding($false)))
	Write-Host "Generated: $outPath"
}




