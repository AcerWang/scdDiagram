import db
import re

data = [['PE1101','I-III母联保护'],
        ['IE1102','II-IV母联智能终端'],
        ['ME1103','分段1123分段测控'],
        ['ME1103','分段I-II分段测控'],
        ['MM2201','分段测控']
        ]

def getInfo():
    p = re.compile(r'([1-9]|[IV]+)-([1-9]|[IV]+).*(\d{4})*')
    
    # db_obj = db.DataBase()
    # sql = '''select name,desc from IED where desc like "%母联%" or desc like "%分段%"'''
    # res = db_obj.select(sql)
    
    info = {}
    v = []
    tag = None

    for name,desc in data:
        name = re.search(r'\d+$',name)
        if name is None:
            continue
        
        name = name.group()[:2]
        if name not in ['10','35','66']:
            name = name+'0'
        
        flag = '分段' if '分段' in desc else '母联'

        desc = p.search(desc)
        if desc is None:
            info[name] = None
            continue
        
        desc = desc.group().split('-')

        if name in info:
            if flag in info[name]:
                info[name][flag].append(desc)
            else:
                v.append(desc)
                info[name][flag] = v
                v = []
        else:
            v.append(desc)
            info[name] = {flag:v}
            v = []
    # db_obj.close_connection()
    return info

if __name__=='__main__':
    info = getInfo()
    print(info)
