#!/usr/bin/env python3
"""Receive and display Controller-UDP packets. No third-party packages required."""

import argparse
import json
import socket
import sys
import time


def pressed(buttons):
    return ",".join(name for name, down in buttons.items() if down) or "-"


def controller(label, data):
    pos = data.get("position", [0, 0, 0])
    stick = data.get("stick", [0, 0])
    return (
        f"{label} tracked={str(data.get('tracked', False)):<5} "
        f"pos=({pos[0]:+.3f},{pos[1]:+.3f},{pos[2]:+.3f}) "
        f"trigger={data.get('trigger', 0):.2f} grip={data.get('grip', 0):.2f} "
        f"stick=({stick[0]:+.2f},{stick[1]:+.2f}) "
        f"buttons=[{pressed(data.get('buttons', {}))}]"
    )


def main():
    parser = argparse.ArgumentParser(description="Receive Quest controller JSON over UDP")
    parser.add_argument("--port", type=int, default=5005, help="UDP listen port (default: 5005)")
    parser.add_argument("--raw", action="store_true", help="Print complete JSON packets")
    parser.add_argument("--once", action="store_true", help="Exit after one packet (useful for testing)")
    args = parser.parse_args()

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    try:
        sock.bind(("0.0.0.0", args.port))
    except OSError as error:
        print(f"Could not listen on UDP {args.port}: {error}", file=sys.stderr)
        return 1

    print(f"Listening on UDP 0.0.0.0:{args.port} (Ctrl+C to stop)", flush=True)
    count = 0
    started = time.monotonic()
    try:
        while True:
            payload, address = sock.recvfrom(65535)
            try:
                packet = json.loads(payload)
            except (UnicodeDecodeError, json.JSONDecodeError) as error:
                print(f"Ignored invalid packet from {address[0]}: {error}", file=sys.stderr)
                continue

            count += 1
            elapsed = max(time.monotonic() - started, 0.001)
            if args.raw:
                print(json.dumps(packet, ensure_ascii=False, separators=(",", ":")), flush=True)
            else:
                print(
                    f"#{packet.get('sequence', '?')} {address[0]} {count / elapsed:.1f} pkt/s | "
                    f"{controller('L', packet.get('left', {}))} | "
                    f"{controller('R', packet.get('right', {}))}",
                    flush=True,
                )
            if args.once:
                break
    except KeyboardInterrupt:
        print("\nStopped.")
    finally:
        sock.close()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
