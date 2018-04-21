import db
import re

def getTransformers():
    '''
       获得变压器台数
    '''
    p = '(#\d主变)|(\d#主变)|(\d号主变)'
    pattn = re.compile(p)

    trans = set()

    sql = "select `desc` from `ied` where `desc` like '%主变%'"
    res = db.select(sql)
    if res is not None:
        for row in res:
            t = pattn.search(row[0])
            if t is None:
                continue
            t = t.group().replace("#","")
            trans.add(t)

    print("主变台数：",len(sorted(trans,reverse=False)))
    for t in trans:
        print(t)

def getBuses():
    '''
       获得线路电压等级
    '''
    p = '([1-3]|[5-7])([0-2]|[5-6])[0-9]{1,}'
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
        print(volt[:3])
    else:
        print(volt)



if __name__=='__main__':
    getTransformers()
    #getBuses()
