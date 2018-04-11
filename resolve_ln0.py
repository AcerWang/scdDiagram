#-*- encoding=utf8 -*-
import db
import xml.etree.ElementTree as ET
import time


start = time.clock()

tree = ET.parse('HS220.scd')
root = tree.getroot()

print(root.tag)
insert_sql = "insert into ln0 values(?,?,?,?,?)"

total,ld_num = 0,0
data = []
for item in root.iter('{http://www.iec.ch/61850/2003/SCL}LDevice'):
        ld_num += 1
        ln0 = item.find('{http://www.iec.ch/61850/2003/SCL}LN0')
        
        if ln0 is not None:
            total += 1
            lnType = ln0.get('lnType')
            lnClass = ln0.get('lnClass')
            inst = ln0.get('inst')
            data.append(("ln0_"+str(total),"ld_"+str(ld_num),lnType,lnClass,inst))

db.insert(insert_sql,data)

print("LD数:%d，LN0数：%d！"%(ld_num,total))

end = time.clock()

print("程序总运行时间：",end-start,"s")
#---- about 1.2 s----#