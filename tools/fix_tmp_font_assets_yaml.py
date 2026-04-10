# One-off / repeatable fix: TMP font .asset YAML from Unity 6 -> 2022.3 compatibility.
# Run: python tools/fix_tmp_font_assets_yaml.py
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

NOTO_TEX_OLD = """  m_ImageContentsHash:
    serializedVersion: 2
    Hash: 00000000000000000000000000000000
  m_IsAlphaChannelOptional: 0
  serializedVersion: 4
  m_Width: 4096
  m_Height: 4096
  m_CompleteImageSize: 16777216
  m_MipsStripped: 0
  m_TextureFormat: 1
  m_MipCount: 1
  m_IsReadable: 1
  m_IsPreProcessed: 0
  m_IgnoreMipmapLimit: 0
  m_MipmapLimitGroupName: 
  m_StreamingMipmaps: 0
  m_StreamingMipmapsPriority: 0
  m_VTOnly: 0
  m_AlphaIsTransparency: 0"""

NOTO_TEX_NEW = """  m_ImageContentsHash:
    serializedVersion: 2
    Hash: 00000000000000000000000000000000
  m_ForcedFallbackFormat: 4
  m_DownscaleFallback: 0
  serializedVersion: 2
  m_Width: 4096
  m_Height: 4096
  m_CompleteImageSize: 16777216
  m_MipsStripped: 0
  m_TextureFormat: 1
  m_MipCount: 1
  m_IsReadable: 1
  m_StreamingMipmaps: 0
  m_StreamingMipmapsPriority: 0
  m_AlphaIsTransparency: 0"""

# CONSOLA .asset uses CRLF line endings on Windows checkouts.
CONSOLA_TEX_OLD = """  m_ImageContentsHash:\r\n    serializedVersion: 2\r\n    Hash: 00000000000000000000000000000000\r\n  m_IsAlphaChannelOptional: 0\r\n  serializedVersion: 4\r\n  m_Width: 1024\r\n  m_Height: 1024\r\n  m_CompleteImageSize: 1048576\r\n  m_MipsStripped: 0\r\n  m_TextureFormat: 1\r\n  m_MipCount: 1\r\n  m_IsReadable: 0\r\n  m_IsPreProcessed: 0\r\n  m_IgnoreMipmapLimit: 1\r\n  m_MipmapLimitGroupName: \r\n  m_StreamingMipmaps: 0\r\n  m_StreamingMipmapsPriority: 0\r\n  m_VTOnly: 0\r\n  m_AlphaIsTransparency: 0"""

CONSOLA_TEX_NEW = """  m_ImageContentsHash:\r\n    serializedVersion: 2\r\n    Hash: 00000000000000000000000000000000\r\n  m_ForcedFallbackFormat: 4\r\n  m_DownscaleFallback: 0\r\n  serializedVersion: 2\r\n  m_Width: 1024\r\n  m_Height: 1024\r\n  m_CompleteImageSize: 1048576\r\n  m_MipsStripped: 0\r\n  m_TextureFormat: 1\r\n  m_MipCount: 1\r\n  m_IsReadable: 0\r\n  m_StreamingMipmaps: 0\r\n  m_StreamingMipmapsPriority: 0\r\n  m_AlphaIsTransparency: 0"""


def downgrade_tmp_sdf_materials(text: str) -> str:
    """TMP_SDF embedded materials: Unity 6 uses serializedVersion 8; 2022 expects 6."""

    def repl(m: re.Match[str]) -> str:
        block = m.group(0)
        if "serializedVersion: 8" not in block:
            return block
        block = block.replace("serializedVersion: 8", "serializedVersion: 6", 1)
        block = re.sub(
            r"(m_Shader: \{fileID: 4800000, guid: 68e6db2ebdc24f95958faec2be5558d6, type: 3\})\r?\n",
            r"\1\n  m_ShaderKeywords: \n",
            block,
            count=1,
        )
        out: list[str] = []
        for line in block.splitlines(True):
            st = line.lstrip()
            if st.startswith("m_Parent:"):
                continue
            if st.startswith("m_ModifiedSerializedProperties:"):
                continue
            if st.startswith("m_ValidKeywords:"):
                continue
            if st.startswith("m_InvalidKeywords:"):
                continue
            if st.startswith("m_LockedProperties:"):
                continue
            if st.startswith("m_BuildTextureStacks:"):
                continue
            if st.startswith("m_AllowLocking:"):
                continue
            out.append(line)
        return "".join(out)

    return re.sub(
        r"--- !u!21 &[0-9\-]+\r?\nMaterial:\r?\n  serializedVersion: 8\r?\n[\s\S]*\Z",
        repl,
        text,
        flags=re.MULTILINE,
    )


def fix_file(path: Path, tex_old: str, tex_new: str, fix_source_font: bool) -> None:
    raw = path.read_bytes()
    if raw.startswith(b"\xef\xbb\xbf"):
        text = raw[3:].decode("utf-8")
        bom = True
    else:
        text = raw.decode("utf-8")
        bom = False

    if "serializedVersion: 8" in text and "Material:" in text:
        text = downgrade_tmp_sdf_materials(text)
    if tex_old not in text:
        raise SystemExit(f"Texture header pattern not found in {path}")
    text = text.replace(tex_old, tex_new, 1)
    if fix_source_font:
        new_ref = "  m_SourceFontFile: {fileID: 12800000, guid: 40fc9b4dd5b03a340b827000bb3df67b, type: 3}"
        for old in (
            "  m_SourceFontFile: {fileID: 0}\r\n",
            "  m_SourceFontFile: {fileID: 0}\n",
        ):
            if old in text:
                text = text.replace(old, new_ref + ("\r\n" if old.endswith("\r\n") else "\n"), 1)
                break

    out = text.encode("utf-8")
    if bom:
        out = b"\xef\xbb\xbf" + out
    path.write_bytes(out)
    print(f"OK {path.relative_to(ROOT)}")


def main() -> None:
    noto = ROOT / "Assets" / "Art" / "Font" / "NotoSansSC-Regular SDF.asset"
    consola = ROOT / "Assets" / "Art" / "Font" / "CONSOLA SDF.asset"
    if NOTO_TEX_OLD in noto.read_text(encoding="utf-8"):
        fix_file(noto, NOTO_TEX_OLD, NOTO_TEX_NEW, fix_source_font=False)
    else:
        print(f"skip (already patched or mismatch) {noto.relative_to(ROOT)}")
    fix_file(consola, CONSOLA_TEX_OLD, CONSOLA_TEX_NEW, fix_source_font=True)


if __name__ == "__main__":
    main()
