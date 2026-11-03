"""DAT Execute DAT: enable Table Change and watch the decoded UDP table."""


def onTableChange(dat, *args):
    language = dat['language', 1].val.strip()
    value = dat['value', 1].val.strip()
    asset_id = dat['assetId', 1].val.strip()
    command = dat['command', 1].val.strip()

    if language == 'en-US':
        langue = 'EN'
        op('language').text = 'E'
    elif language == 'zh-CN':
        langue = 'CH'
        op('language').text = 'C'
    else:
        print('未知语言:', language)
        return

    if value == 'layout':
        op('switch1').par.index = 0
    elif value in ('business-scenes', 'institutions'):
        op('cue').par.file = 'sc/S/' + langue + '/logo.mp4'
        op('switch1').par.index = 1
    elif value == 'social-mainland':
        op('cue').par.file = 'sc/P/' + langue + '/社会价值1.png'
        op('switch1').par.index = 1
    elif value == 'social-hk':
        op('cue').par.file = 'sc/P/' + langue + '/社会价值2.png'
        op('switch1').par.index = 1

    # 素材 ID 需要包含 S/ 或 P/，例如 sc/S/logo 或 sc/P/社会价值1。
    lookup_id = asset_id if command == 'play' else value if command == 'language' and '/' in value else ''
    if lookup_id:
        parts = lookup_id.replace('\\', '/').strip('/').split('/')
        media_dir = next((part.upper() for part in parts if part.upper() in ('S', 'P')), None)
        if media_dir is None:
            print('素材 ID 中没有 S/ 或 P/:', lookup_id)
            return
        name = parts[-1]
        folder = op('folder1')
        if folder is None:
            print('找不到 folder1')
            return

        folder.par.rootfolder = 'SC/' + media_dir + '/' + langue
        folder.cook(force=True)

        for row in folder.rows()[1:]:
            path = row[1].val
            if row[0].val == name:
                op('cue').par.file = path
                op('switch1').par.index = 1
                print(path)
                return
        print('未找到素材:', lookup_id, langue)
    return


def onRowChange(dat, rows):
    return


def onColChange(dat, cols):
    return


def onCellChange(dat, cells, prev):
    return


def onSizeChange(dat):
    return
