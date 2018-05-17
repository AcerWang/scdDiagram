from DBHelper import DataBase
import re
import json

def conv2json(t=None,v=None,b=None,l=None,l_b=None):
    '''
        将获得的数据转换为json格式
	'''
    m = {'Trans':t,'Voltage':v,'Bus':b,'Line':l,"Line-Bus":l_b}
    j_data = json.dumps(m,ensure_ascii=False)
    return j_data

def getTransformers(db):
    '''
       获得变压器台数
    '''
    p = r'(#\d主变)|(\d#主变)|(\d号主变)'
    pattn = re.compile(p)

    trans = set()

    sql = "select `desc` from `ied` where `name` like '%T%' and `desc` like '%主变%'"
    res = db.select(sql)
    if res is not None:
        for row in res:
            t = pattn.search(row[0])
            if t is None:
                continue
            t = t.group().replace("#","")
            trans.add(t)
    return list(trans)

def getVolts(db):
    '''
       获得电压等级
    '''
    p = r'([1-3]|[5-7])([0-2]|[5-6])[0-9]{1,}'
    pattn = re.compile(p)

    volt = set()
    
    sql = "select name from ied"
    res = db.select(sql)
    for name in res:
        s = pattn.search(name[0])
        if s is not None:
            v = int(s.group()[:2]) 
            if v != 10 and v != 35 and v != 66:
                volt.add(v*10)
            else:
                volt.add(v)
    volt = sorted(list(volt),reverse=True)
    if len(volt)>3:
        return volt[:3]
    else:
        return volt

def getBuses(db):
    '''
       获得母线数量
    '''
    p = r'\d{3,}'
    pattn = re.compile(p)

    buses = set()

    sql = 'select name,desc from ied where name like "%CM%" or name like "%PM%"'
    res = db.select(sql)

    for bus in res:
        b = pattn.search(bus[0])
        if b is not None:
            b = b.group()
            if len(b)==4 and int(b[-2:])>10 :
                continue
            buses.add(b)
    sorted(buses)
    return list(buses)

def getLines(db):
    '''
       获得线路数
    '''
    p = r'(\d{2,4}\D{2,}\d?\D*?线路?\d*)|(\D*线\d*)'
    pattn = re.compile(p)

    lines = dict()
    sql = "select name,desc from ied where name like '%L%' and desc like '%线%'"

    res = db.select(sql)

    for line in res:
        key = re.search(r'\d{3,}',line[0])
        value = pattn.search(line[1])
        if key is not None and value is not None:
            if key.group() in lines:
                continue
            if '在线' in value.group() or '消弧' in value.group():
                continue
            lines[key.group()] = value.group()
    return lines

def getMU(db):
    '''
        获得线路合并单元
    '''
    sql = 'select name,desc from ied where desc like "%合%" and name like "%M%L%"'
    db.select(sql)

def getLine_Bus(db):
    '''
        获得母线上的线路
    '''
    p = re.compile(r'([1-9]|[IV]+)-([1-9]|[IV]+)|(\d{4})')
    
    sql = '''select name,desc from IED where desc like "%母联%" or desc like "%分段%"'''
    data = db.select(sql)
    
    info = {}
    v = []

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
            info[name] = {flag:None}
            continue
        
        desc = desc.group().split('-')

        if name in info:
            if flag in info[name]:
                if desc in info[name][flag]:
                    pass
                else:
                    info[name][flag].append(desc)
            else:
                v.append(desc)
                info[name][flag] = v
                v = []
        else:
            v.append(desc)
            info[name] = {flag:v}
            v = []
    return info
    
if __name__=='__main__':
    import time
    start = time.clock()
    db = DataBase()

    t = getTransformers(db)
    v = getVolts(db)
    b = getBuses(db)
    l = getLines(db)
    l_b = getLine_Bus(db)

    jsonstr = conv2json(t,v,b,l,l_b)

    print(jsonstr)
    with open('res/data.json','w') as f:
        f.write(jsonstr)
    
    db.close_connection()
    end = time.clock()
    print('total time:',end-start,'s')