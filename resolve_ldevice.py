#-*- encoding=utf8 -*-
import db
import xml.etree.ElementTree as ET
import time


start = time.clock()

tree = ET.parse('HS220.scd')
root = tree.getroot()

print(root.tag)
insert_sql = "insert into ldevice values(%s,%s,%s,%s)"

total = 0
ap_num = 0
data = []
for item in root.iter('{http://www.iec.ch/61850/2003/SCL}AccessPoint'):
    ap_num += 1
    for ld in item.iter('{http://www.iec.ch/61850/2003/SCL}LDevice'):
        desc = ld.get('desc')
        inst = ld.get('inst')
        total += 1
        data.append((total,ap_num,inst,desc))

print("AP数：%d，LDevice数：%d"%(ap_num,total))
db.insert(insert_sql,data)
#print("总共插入%d条数据！"%total)

end = time.clock()

print("程序总运行时间：",end-start,"s")