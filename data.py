import db
import re
import time
import json

def conv2json(t=None,v=None,b=None,l=None,l_b=None):
    '''
    convert data to json format 
    # t = ['1#','2#']
    # v = ['110','220','35']
    # b = ['1101','1102','2201','2202','3501','3502']
    # l = {'1101':'110#1','1102':'110#2','1103':'110#3','2201':'220#1','2202':'220#2','2203':'220#3','3501':'35#1','3502':'35#2','3503':'35#3'}
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

def which(db):
    '''
        获得线路所在母线
    '''
    sql = """select DISTINCT tmp.Line,tmp.Ref_To,LN.desc from tmp
            INNER JOIN LN on LN.inst=tmp.lnInst and LN.lnClass=tmp.lnClass 
            where LN.ldevice_id = (select IEDTree.LD_id FROM IEDTree where IEDTree.IED=tmp.Ref_To and IEDTree.LDevice=tmp.ldInst)
        """
    
    res = db.select(sql)
    line_bus = {}
    desc = list()
    p = re.compile(r'(\d+)|(I)|(II)|(III)|(IV)|(V)|(VI)|(VII)|(VIII)')
    for i in res:
            
        key = p.search(i[0]).group()
        value = p.search(i[2]).group()
        if not i[1].startswith('M'):
            line_bus[key] = None
            continue
        if key in line_bus:
            if value in desc:
                continue
            line_bus[key].append(value)
        else:
            desc = list()
            line_bus[key] = desc
        
    #print(line_bus)
    return line_bus
    
if __name__=='__main__':
    
    start = time.clock()
    database = db.DataBase()

    t = getTransformers(database)
    v = getVolts(database)
    b = getBuses(database)
    l = getLines(database)
    l_b = which(database)

    jsonstr = conv2json(t,v,b,l,l_b)

    print(jsonstr)
    with open('res/data.json','w') as f:
        f.write(jsonstr)
    
    database.close_connection()
    end = time.clock()
    print('total time:',end-start,'s')