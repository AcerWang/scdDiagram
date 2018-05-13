import db
import re
import time
from conv2json import conv2json

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


    
if __name__=='__main__':
    
    start = time.clock()
    database = db.DataBase()

    t = getTransformers(database)
    v = getVolts(database)
    b = getBuses(database)
    l = getLines(database)

    jsonstr = conv2json(t,v,b,l)
    print(jsonstr)
    with open('data.json','w') as f:
        f.write(jsonstr)
    
    database.close_connection()
    end = time.clock()
    print('total time:',end-start,'s')