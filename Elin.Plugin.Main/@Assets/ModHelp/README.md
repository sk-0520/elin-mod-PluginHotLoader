# Mod Help 用原本

[Mod Help](https://steamcommunity.com/workshop/filedetails/?id=3406542368) に読み込ませるヘルプファイルの元ファイルと、ゲーム起動なしに確認するためのテンプレートです。

`index.xhtml` に以下ルールで記述することで Mod ビルド時に自動生成されます。

不要な場合は `ModHelp` ディレクトリか、 `index.xhtml` を削除してください。

## ルール

| TAG | ModHelp |説明 |
|---|---|---|
| `help:page` | - | ページ単位のブロックとして使用 |
| `help:title` | `$` | ページのタイトル |
| `help:p` | - | 論理的な行 |
| `help:style` | `color`, `size`, `i`, `b` | テキスト装飾 |
| `help:text` | - | 文字列。 `jp`, `en`, `cn` 等の Elin 内で使用される言語属性(小文字)を使用して多言語化 |
| `help:topic` | `{topic` | トピックヘッダー |
| `help:pair` | `{pair` | キーと値のペア |
| `help:key` | `{pair\|**key**` | キー |
| `help:value` | `{pair\|key\|**value**` | 値 |
| `help:list` | - | 論理的なリストブロック |
| `help:item` | ・ | リストアイテム |
| `help:qa` | - | Q&A |
| `help:q` | `{Q` | 質問 |
| `help:a` | `{A` |  答え |
| `help:link` | `{link` | リンク |
| `help:image` | `{image` | 画像(@Assets/Texture 参照) |
| `help:br` | - | 改行 |

---

なぜ今さら XHTML で書いているかというと、ビルド処理で使用する Powershell(これも Windows 標準搭載版の古いやつ) が HTML を解析できるのか不明＋XMLなら大丈夫でしょ、という甘い考えのもと XML が強制されブラウザでいい感じに表示できるという利点も後押しして採用。

---

途中まで手で書いてたけどしんどくなって Copilot 君に書いてもらったので保守不能。
バグがあったらスクラッチビルド。
