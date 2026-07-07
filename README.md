# DLsiteMP3TagEditor
DLsite・FANZA同人の作品情報を元にmp3ファイルのタグを置き換えるツール
![image](https://github.com/dekotan24/DLsiteMP3TagEditor/assets/27037519/352dedfd-bed2-4180-ac8a-916904a75585)


## 開発環境
Visual Studio 2022

.NET 6.0


## 使い方
<a href="https://twitter.com/r4adb/status/1713377144011976785" target="_blank">ツイート</a>を参照

### 検索できる形式
- DLsite：作品ID（`RJ343328`、数字のみでも可）またはURL
  - AI生成作品（aix）や美少女ゲーム（VJ）にも対応
- FANZA同人：作品ID（`d_750863`）またはURL（`https://www.dmm.co.jp/dc/doujin/-/detail/=/cid=d_750863/`）

### ダークモード（Ver.1.6〜）
画面右下の「ダークモード / ライトモード」ボタンで切り替えられます。設定は次回起動時にも引き継がれます。

※販売日欄のドロップダウンカレンダーはOS描画のため、ダークモードでも明色のままです。

## 注意
一部のセキュリティ対策ソフトウェアでは、本アプリの挙動がランサムウェアに似ている（多くのファイルを一気に更新するため）ということでブロックされる場合があります。
![image](https://github.com/dekotan24/DLsiteMP3TagEditor/assets/27037519/3ba9ba1b-e16c-43eb-89d8-47db65a241b8)

少数のファイルであれば問題ないと思われますが、例外に追加すると安全に使用できます。



## 依存パッケージ
・[DLsiteInfoGetter](https://github.com/dekotan24/DLsiteInfoGetter)
