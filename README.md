# Controller UDP for Quest 3

Quest 3の左右コントローラー情報を、同一LAN上へJSON/UDPで送信するシンプルなUnityアプリです。

## 現在の機能

- 左右コントローラーの6DoF姿勢を72 Hzで送信
- 位置、回転、速度、角速度を送信
- スティック、トリガー、グリップ、各ボタンの状態を送信
- `255.255.255.255:5005`へのUDPブロードキャスト
- Quest 3のパススルー表示
- コントローラー位置へのRGB座標軸表示機能
  - X軸: 赤
  - Y軸: 緑
  - Z軸: 青
  - 各軸は長さ5cm。Quest実機の左右の視野で表示確認済み

シーンへコンポーネントを配置する必要はありません。実行時に必要なオブジェクトが自動生成されます。

## 使用環境

- Unity 6000.6.1f1
- Android / ARM64 / IL2CPP
- Unity OpenXR Plugin
- Meta XR Core SDK
- Meta Quest 3

## Quest向けビルド

1. Quest 3をUSB接続し、ヘッドセット内でUSBデバッグを許可します。
2. Unityでプロジェクトを開きます。
3. 必要なら `Tools > Controller UDP > Configure Quest 3` を実行します。
4. `File > Build Profiles` を開きます。
5. Androidプロファイルを選択し、接続中のQuest 3へ `Build And Run` します。

生成されるAPKの標準パスは次のとおりです。

```text
Builds/Quest/ControllerUDP.apk
```

サイドロードしたアプリは、Questのアプリライブラリにある「提供元不明」から起動できます。

## UDP受信

Windowsでは次を実行します。

```powershell
Tools\receive.cmd
```

Git Bash、macOS、Linuxでは次を実行します。

```bash
bash Tools/receive.sh
```

完全なJSONを表示する場合:

```powershell
Tools\receive.cmd --raw
```

別ポートで受信する場合:

```powershell
Tools\receive.cmd --port 5006
```

PCのファイアウォールでは、UDP 5005番ポートの受信を許可してください。

## 送信先

初期値は同一LAN全体へのブロードキャストです。

```text
255.255.255.255:5005
```

通常はPCのIPアドレスを指定する必要はありません。ネットワーク機器がブロードキャストを遮断する場合は、`ControllerUdpSender.cs`の`destinationAddress`をPCのIPv4アドレスへ変更できます。

起動引数でも変更できます。

```text
--udp-address 192.168.1.100 --udp-port 5005
```

## データ形式

各パケットはUTF-8のJSONです。主な項目:

- `version`
- `sequence`
- `timestampMs`
- `left`
- `right`

左右それぞれに、接続・追跡状態、位置、Quaternion回転、速度、角速度、スティック、トリガー、グリップ、ボタン状態が含まれます。

UDP形式は`version: 2`。座標は右手系のX前・Y左・Z上で、位置はメートル単位、回転はQuaternionの`x, y, z, w`です。

左右のローカル姿勢は、取得した姿勢から右手系のローカルY軸（左）まわりに+45度補正します。補正はコントローラーのローカル軸基準です。送信クォータニオン・手元の表示軸・右コントローラーから取るcalib姿勢に同じ補正を適用し、位置はそのまま使います。

左右のGripを同時に握るとキャリブレーションします。原点と回転は、その瞬間の右コントローラーの位置と姿勢をそのまま使います。傾き・ひねりも含め、+Xはその姿勢の前、+Yは左、+Zは上です。+Zは鉛直上とは限りません。HMDの向きは使用しません。位置・回転・速度・角速度すべてを同じ座標系へ変換します。

calib前はUDPを送信しません。握り続けても再設定せず、どちらかを離して再び両方握ると再設定します。両方の追跡が有効なことが必要です。真上・真下を向けた姿勢でも設定できます。設定はアプリ終了まで有効です。アプリを再起動すると再calibが必要です。calib直後の右コントローラーは位置`[0,0,0]`、回転`[0,0,0,1]`です。

`calibrated`と`calibrationRevision`で設定状態・設定回数を確認できます。原点は右コントローラーから取る1つだけです。左右それぞれのStick Clickを押している間だけ、その手の送信位置・回転を取得・更新します。離すと最後の値を保持します。calibだけでは左・右の送信姿勢は取得しません。未取得の手は`poseValid:false`、`position:null`、`rotation:null`で、不透明軸は非表示、最新姿勢の半透明軸は表示します。再calibで以前の保持姿勢を解除します。ボタン・スティック入力は常時更新し、保持中の速度・角速度はゼロ、`poseUpdating`はfalseになります。

画面では、不透明RGB軸が実際に送信している保持姿勢、25%不透明度のRGB軸がコントローラーの最新位置・姿勢です。双方5cm（赤=X前、緑=Y左、青=Z上）で、一致する場合は重なります。軸のそばに左右のSTICK（Stick Click）・TRIGGER・GRIPのON/OFFを小さく表示します。UDPのクォータニオンは、固定された原点の軸から見た保持姿勢の回転を表します。RPYは送信しません。

キャリブレーションすると、設定した原点にも5cmのRGB軸と白い原点マーカーが表示されます。この軸は設定した位置と向きに固定され、手を動かしても追従しません。再キャリブレーションすると新しい原点・向きへ移動します。

## 主なファイル

- `Assets/ControllerUdp/ControllerUdpSender.cs`: 入力取得とUDP送信
- `Assets/ControllerUdp/ControllerPoseVisualizer.cs`: RGB座標軸表示
- `Assets/ControllerUdp/QuestPassthrough.cs`: Questパススルー設定
- `Assets/ControllerUdp/ControllerUdpBootstrap.cs`: 実行時の自動生成
- `Tools/receive_udp.py`: UDP受信プログラム
- `Tools/receive.cmd`: Windows用起動スクリプト
- `Tools/receive.sh`: Bash用起動スクリプト
