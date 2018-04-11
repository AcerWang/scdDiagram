#-*- encoding=utf8 -*-
import db
import xml.etree.ElementTree as ET
import time


start = time.clock()

tree = ET.parse('HS220.scd')
root = tree.getroot()


total,ds_num = 0,0
insert_sql = "insert into fcda values(?,?,?,?,?,?,?,?)"
data = []
for item in root.iter('{http://www.iec.ch/61850/2003/SCL}DataSet'):
    ds_num += 1
    for fcda in item.iter('{http://www.iec.ch/61850/2003/SCL}FCDA'):
        total += 1
        ldInst = fcda.get('ldInst')
        prefix = fcda.get('prefix')
        lnInst = fcda.get('lnInst')
        lnClass = fcda.get('lnClass')
        doName = fcda.get('doName')
        fc = fcda.get("fc")
        data.append(("fcda_"+str(total),"ds_"+str(ds_num),ldInst,prefix,lnInst,lnClass,doName,fc))

db.insert(insert_sql,data)
print("DataSet数：%d，FCDA数：%d！"%(ds_num,total))

end = time.clock()

print("程序总运行时间：",end-start,"s")
#----about 2.0 s----#