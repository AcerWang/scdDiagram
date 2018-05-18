from DBHelper import DataBase
import re
import json

dig_index = ['1','2','3','4','5','6','7','8','9']
char_index = ['I','II','III','IV','V','VI','VII','VIII','IX']

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
    m = {}
    s = []
    for bus in buses:
        volt = bus[:2]
        volt = volt if volt in ['10','35','66'] else volt+'0'
        if volt in m:
            m[volt].append(int(bus[-1]))
        else:
            s.append(int(bus[-1]))
            m[volt] = s
            s = []
    
    return m

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

def getBusRelationship(db):
    '''
        获得母线连接关系
    '''
    p = re.compile(r'([1-9]|[IVX]+)-([1-9]|[IVX]+)|([123567][012356][1-9]{2})')
    
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
        
        desc = desc.group().split('-') if '-' in desc.group() else list(desc.group()[-2])
        i = 0
        for x in desc:
            if x in char_index:
                desc[i] = char_index.index(x)+1
                i += 1
            else:
                desc[i] = dig_index.index(x)+1
                i += 1

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
    
def getLineBus(db):
    '''
        获得线路与母线关系
    '''
    p_bus = re.compile(r'[1-9]|[IVX]+')
    p_line = re.compile(r'\d{3,4}')

    sql = '''select DISTINCT tmp.Line,LN.desc from tmp 
            INNER JOIN LN on LN.inst=tmp.lnInst and LN.lnClass=tmp.lnClass 
            where LN.ldevice_id = (select IEDTree.LD_id FROM IEDTree where IEDTree.IED=tmp.Ref_To and IEDTree.LDevice=tmp.ldInst)'''
    res = db.select(sql)
    
    if res is None:
        return None
    
    m = {}
    b = []
    for data in res:
        line = p_line.search(data[0]).group()
        bus = p_bus.search(data[1]).group()

        bus = dig_index.index(bus)+1 if bus in dig_index else char_index.index(bus)+1
        if line in m:
            if bus in m[line]:
                continue
            m[line].append(bus)
        else:
            b.append(bus)
            m[line] = b
            b = []
    return m

if __name__=='__main__':
    import time
    start = time.clock()
    db = DataBase()

    # t = getTransformers(db)
    # v = getVolts(db)
    #b = getBuses(db)
    #m = getLineBus(db)
    # l = getLines(db)
    l_b = getBusRelationship(db)
    print(l_b)

    # jsonstr = conv2json(t,v,b,l,l_b)

    # print(jsonstr)
    # with open('res/data.json','w') as f:
    #     f.write(jsonstr)
    
    db.close_connection()
    end = time.clock()
    print('total time:',end-start,'s')