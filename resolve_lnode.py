#-*- encoding=utf8 -*-
import db
import xml.etree.ElementTree as ET
import time


start = time.clock()

tree = ET.parse('HS220.scd')
root = tree.getroot()

print(root.tag)
insert_sql = "insert into lnode values(?,?,?,?,?,?,?)"

total = 0
ld_num = 0
data = []

for item in root.iter('{http://www.iec.ch/61850/2003/SCL}LDevice'):
    ld_num += 1
    for ln in item.iter('{http://www.iec.ch/61850/2003/SCL}LN'):
        total += 1
        lnType = ln.get('lnType')
        lnClass = ln.get('lnClass')
        inst = ln.get('inst')
        prefix = ln.get('prefix')
        desc = ln.get('desc')
        data.append(("ln_"+str(total),"ld_"+str(ld_num),lnType,lnClass,prefix,inst,desc))

print("LDevice数：%d，LNode数：%d"%(ld_num,total))
db.insert(insert_sql,data)

end = time.clock()

print("程序总运行时间：",end-start,"s")
#----about 1.4 s----#