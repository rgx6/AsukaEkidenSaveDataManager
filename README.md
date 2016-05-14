# アスカ駅伝セーブデータ管理ツール

アスカ駅伝のセーブデータ受け渡しに伴う煩雑な作業を少しだけ自動化します。

## ワンクリックでできること

* 既存セーブデータのバックアップ
* 前走者から受け取ったバトンの取り込み
  * zip ファイルを解凍
  * セーブデータをインポート
  * レジストリを削除
* 次走者へのバトンを作成
  * セーブデータを zip 圧縮
* バックアップしたセーブデータの復元

## できないこと

* バトンのダウンロード
* バトンのアップロード
* セーブデータ 1 の操作

## 動作環境

* Windows 7 + .NET Framework 4 以降
* 動作確認はしていませんが Vista とか 8 とか 10 でも動く気がします。

## 使い方

1. `AsukaEkidenSaveDataManager.exe` を起動します。  
   レジストリを操作する関係で管理者権限が必要なので、ユーザーアカウント制御のダイアログが表示されることがあります。  
   このアプリを信用できるなら「はい」を押してください。

2. 「セーブデータ保存フォルダ」を指定してください。  
   初回起動時に適当にあたりをつけて設定しますが、念のため確認はしておいてください。

3. (セーブデータ 2 に自分のデータがある場合のみ)  
   「バックアップ」ボタンを押します。  
    セーブデータと同じフォルダにバックアップファイル `ASUKA____002.bak` を作成します。

4. 「インポート」を押します。  
   ファイル選択ダイアログが表示されるので、ダウンロードしたセーブデータ (`ASUKA____002` または zip ファイル) を指定してください。  
   zip 以外の圧縮形式やパスワード付きの zip には対応していないので、あらかじめ解凍しておいてください。

5. (ゲーム内) セーブデータが正しくインポートされているか確認します。  
   インポートが完了したのにゲーム内に反映されていない場合は「セーブデータ保存フォルダ」の設定が間違っている可能性があります。

6. **(走り終わってセーブしたら)** 「エクスポート」を押します。  
   セーブデータを zip 形式で圧縮します。  
   エクスプローラーが起動し `ASUKA____002.zip` が表示されるので、このファイルをアップローダにアップしてください。  
   設定によっては拡張子の `.zip` は表示されないかもしれません。

7. **(次走者がバトンを正しく受け取ったことを確認したら)** 「復元」ボタンを押します。  
   バックアップしたセーブデータを復元します。

## インストール

[ここ](https://github.com/rgx6/AsukaEkidenSaveDataManager/releases)
から最新の `AsukaEkidenSaveDataManager_v*.*.*.zip` をダウンロード、解凍し、適当なフォルダに置いてください。

## エラーで落ちたら

このツールをインストールしたフォルダに `error.txt` というファイルが作成されるはずなので、内容を issue とか [Twitter](https://twitter.com/rgx_6) とかに送ってもらえればなんとかするかもしれません。