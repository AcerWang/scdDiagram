#-*- encoding=utf8 -*-
import db
import xml.etree.ElementTree as ET
import time


start = time.clock()

tree = ET.parse('HS220.scd')
root = tree.getroot()


total,ln0_num = 0,0
insert_sql = "insert into GSEControl values(?,?,?,?,?,?,?)"
data = []
for item in root.iter('{http://www.iec.ch/61850/2003/SCL}LN0'):
    ln0_num += 1
    
    for gsec in item.iter('{http://www.iec.ch/61850/2003/SCL}GSEControl'):
        total += 1
        appId = gsec.get('appId')
        datSet = gsec.get('datSet')
        cnfRev = gsec.get('cnfRev')
        name = gsec.get('name')
        Type = gsec.get('type')
        data.append(("gse_"+str(total),"ln0_"+str(ln0_num),appId,datSet,cnfRev,name,Type))

db.insert(insert_sql,data)
print("LN0数：%d，GSEControl数：%d！"%(ln0_num,total))

end = time.clock()

print("程序总运行时间：",end-start,"s")
#----about 2.0 s----#