"""UDP In DAT callback: parse one Unity packet and show it in text1."""
import json


def onReceive(dat, rowIndex, message, bytes, peer):
    try:
        raw = bytes.decode("utf-8-sig").replace("\x00", "").strip() if bytes else str(message).strip()
        packet = json.loads(raw)
        if not isinstance(packet, dict):
            raise ValueError("JSON 顶层不是对象")
        output = op("text1")
        if output is None:
            debug("找不到 Text DAT: text1", packet)
        else:
            output.text = json.dumps(packet, ensure_ascii=False, indent=2)

    except Exception as e:
        output = op("text1")
        error_text = "UDP 解析失败: {}\n原始消息: {}".format(repr(e), str(message))
        if output is not None:
            output.text = error_text
        else:
            debug(error_text)
    return
