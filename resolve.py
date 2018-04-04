#-*- encoding=utf8 -*-
import db
import xml.etree.ElementTree as ET
import time


start = time.clock()

tree = ET.parse('HS220.scd')
root = tree.getroot()

print(root.tag)
total = 0
insert_sql = "insert into ied values(%s,%s,%s,%s,%s)"
ieds = []
for item in root.iter('{http://www.iec.ch/61850/2003/SCL}IED'):
    total += 1
    #print(item.find('title').text)
    name = item.get('name')
    desc = item.get('desc')
    Type = item.get('type')
    manufacturer = item.get('manufacturer')
    version = item.get('configVersion')
    ieds.append((name,desc,Type,manufacturer,version))

db.insert(insert_sql,ieds)
print("总共%d条数据！"%total)

end = time.clock()

print("程序总运行时间：",end-start,"s")