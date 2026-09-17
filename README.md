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
  - 現時点ではQuest実機で座標軸が表示されないことがあります

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

位置はOpenXRのローカルトラッキング空間におけるメートル単位です。回転はQuaternionの`x, y, z, w`です。

## 主なファイル

- `Assets/ControllerUdp/ControllerUdpSender.cs`: 入力取得とUDP送信
- `Assets/ControllerUdp/ControllerPoseVisualizer.cs`: RGB座標軸表示
- `Assets/ControllerUdp/QuestPassthrough.cs`: Questパススルー設定
- `Assets/ControllerUdp/ControllerUdpBootstrap.cs`: 実行時の自動生成
- `Tools/receive_udp.py`: UDP受信プログラム
- `Tools/receive.cmd`: Windows用起動スクリプト
- `Tools/receive.sh`: Bash用起動スクリプト
