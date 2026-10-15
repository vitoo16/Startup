"""Minimal deterministic RGBA PNG writer reused across first-party pixel drafts.

Rows are top-to-bottom, each row exactly width*4 RGBA bytes; no Pillow,
Photoshop/stock sprite, online inputs, font file or image-generation dependency.
"""
import struct
import zlib


def png_rgba(width, height, rows):
    if type(width) is not int or type(height) is not int or not (0 < width <= 16384 and 0 < height <= 16384):
        raise ValueError("Invalid PNG dimensions")
    rows = list(rows)
    if len(rows) != height or any(len(row) != width * 4 for row in rows):
        raise ValueError("Invalid RGBA rows")

    def chunk(kind, payload):
        return struct.pack("!I", len(payload)) + kind + payload + struct.pack(
            "!I", zlib.crc32(kind + payload) & 0xffffffff)
    image = b"".join(b"\x00" + bytes(row) for row in rows)
    return (b"\x89PNG\r\n\x1a\n" +
            chunk(b"IHDR", struct.pack("!2I5B", width, height, 8, 6, 0, 0, 0)) +
            chunk(b"IDAT", zlib.compress(image, 9)) + chunk(b"IEND", b""))
