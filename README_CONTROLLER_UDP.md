# Quest 3 Controller UDP

Quest 3 の左右コントローラ状態を、72 Hz の UTF-8 JSON/UDP で送信する最小構成です。
シーンへの追加作業は不要です。

## 使い方

1. `Assets/ControllerUdp/ControllerUdpSender.cs` 冒頭の `destinationAddress` を受信PCのIPv4アドレスに変更します（初期値 `192.168.1.100`、ポート `5005`）。
2. Unityを開き、パッケージの読み込み完了を待ちます。
3. 必要なら `Tools > Controller UDP > Configure Quest 3` を一度実行します。
4. `File > Build Profiles` からAndroid/Quest 3へBuild And Runします。
5. Quest 3と受信PCを同じLANに接続し、PCのファイアウォールでUDP 5005を許可します。

送信先はAPK起動引数 `--udp-address 192.168.x.x --udp-port 5005` でも上書きできます。

## 受信確認（Python）

```python
import json, socket

s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
s.bind(("0.0.0.0", 5005))
while True:
    data, sender = s.recvfrom(65535)
    packet = json.loads(data)
    print(sender, packet["left"]["position"], packet["right"]["buttons"])
```

姿勢はOpenXRのローカルトラッキング空間で、位置はメートル、回転はQuaternion `[x,y,z,w]` です。
各パケットには `sequence`, UTCの `timestampMs`, 左右の接続/追跡状態、位置、回転、速度、角速度、trigger、grip、スティック、各ボタンが含まれます。
