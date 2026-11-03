"""TouchDesigner Text DAT: name this operator command_router.

The UDP callback calls handle(packet) after decoding Unity's JSON packet.
All fields remain available through op('command_router').fetch('last_event').
"""

# 每台播放电脑可填写自己的 targetId，例如 "layout-3"；留空则接收所有目标。
EXPECTED_TARGET_ID = ""

# 将 Switch TOP 的各输入接好，再填写其节点路径和素材 ID 对应的输入序号。
SWITCH_TOP_PATH = "switch1"
ASSET_TO_INDEX = {
    # "S/001": 0,
    # "S/002": 1,
}

# Folder DAT 需开启 Include Subfolders，并勾选 Base Name、Path 两列。
FOLDER_DAT_PATH = "folder1"
LANGUAGE_FOLDER = {"zh-cn": "CH", "en-us": "EN"}


def handle(packet):
    """解析后的每一条 UDP 指令只调用一次。返回处理后的字段字典。"""
    if not isinstance(packet, dict):
        raise ValueError("UDP 数据必须是 JSON 对象")

    event = {
        "command": str(packet.get("command") or "").strip(),
        "targetId": str(packet.get("targetId") or "").strip(),
        "assetId": str(packet.get("assetId") or "").strip(),
        "value": str(packet.get("value") or "").strip(),
        "language": str(packet.get("language") or "zh-CN").strip(),
        "sentAtUnixMs": packet.get("sentAtUnixMs"),
    }
    command = event["command"]
    if not command:
        raise ValueError("UDP 数据缺少 command")
    if EXPECTED_TARGET_ID and event["targetId"] != EXPECTED_TARGET_ID:
        return None

    # 这些值可以从其他 DAT 或参数表达式中直接读取。
    me.store("last_event", event)
    me.store("language", event["language"])
    me.store("command", command)

    if command == "play":
        if not event["assetId"]:
            raise ValueError("play 指令缺少 assetId")
        me.store("assetId", event["assetId"])
        me.store("assetPath", find_asset_path(event["assetId"], event["language"]) or "")
        me.store("playback", "playing")
        if ASSET_TO_INDEX:
            switch_asset(event["assetId"])
    elif command == "play-core":
        me.store("playback", "playing")
    elif command == "pause":
        me.store("playback", "paused")
    elif command == "replay":
        me.store("playback", "playing")
        # 重播的时间归零逻辑可在这里接入实际播放器。
    elif command == "volume":
        me.store("volume", max(0.0, min(1.0, float(event["value"]))))
    elif command in ("mute", "amplifier"):
        me.store(command, event["value"].lower() == "true")
    elif command == "back":
        me.store("page", event["value"])  # category 或 layout
    elif command == "select-layout":
        me.store("layout", event["targetId"])
    elif command == "select-category":
        me.store("category", event["value"])
    elif command == "language":
        # 语言切换后，重新查找当前素材对应的 CH / EN 文件。
        current_asset = me.fetch("assetId", "")
        if current_asset:
            me.store("assetPath", find_asset_path(current_asset, event["language"]) or "")
    elif command == "wake":
        me.store("awake", True)
    else:
        debug("未设置专用处理的 UDP 指令:", command, event["value"])

    return event


def find_asset_path(asset_id, language="zh-CN"):
    """如 sc/logo，按目录 sc、basename logo 和语言查找 Folder DAT 的完整路径。"""
    folder = op(FOLDER_DAT_PATH)
    if folder is None:
        debug("找不到 Folder DAT:", FOLDER_DAT_PATH)
        return None

    header_row = folder.row(0)
    if not header_row:
        debug("Folder DAT 没有表头:", FOLDER_DAT_PATH)
        return None
    headers = [cell.val.lower() for cell in header_row]
    if "basename" not in headers or "path" not in headers:
        debug("Folder DAT 需要 basename 和 path 两列:", FOLDER_DAT_PATH)
        return None
    basename_col = headers.index("basename")
    path_col = headers.index("path")

    id_parts = str(asset_id).replace("\\", "/").strip("/").split("/")
    name = id_parts[-1].lower()
    directory = id_parts[0].lower() if len(id_parts) > 1 else ""
    language_directory = LANGUAGE_FOLDER.get(str(language).lower(), "").lower()
    matches = []
    for row in folder.rows()[1:]:
        path = row[path_col].val
        path_parts = [part.lower() for part in path.replace("\\", "/").split("/")]
        if row[basename_col].val.lower() != name:
            continue
        if directory and directory not in path_parts:
            continue
        if language_directory and language_directory not in path_parts:
            continue
        matches.append(path)

    if len(matches) == 1:
        return matches[0]
    if len(matches) > 1:
        debug("素材匹配到多个文件，请缩小 Folder DAT 范围:", asset_id, matches)
    else:
        debug("Folder DAT 中没有找到素材:", asset_id, language)
    return None


def switch_asset(asset_id):
    """按 assetId 切换素材；只需填写上方 ASSET_TO_INDEX。"""
    index = ASSET_TO_INDEX.get(asset_id)
    if index is None:
        debug("未配置素材的 Switch 输入序号:", asset_id)
        return False
    switch = op(SWITCH_TOP_PATH)
    if switch is None:
        debug("找不到 Switch TOP:", SWITCH_TOP_PATH)
        return False
    switch.par.index = index
    return True
