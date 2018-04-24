#-*- encoding=utf8 -*-
import db
import xml.etree.ElementTree as ET
import time


start = time.clock()

items = ET.iterparse('scd/HSB.scd',events=('start','end'))


total = 0
insert_sql = "insert into IED values(?,?,?,?,?,?)"
ieds = []

for event,ele in items:
    if event == 'start':
        if ele.tag =='{http://www.iec.ch/61850/2003/SCL}IED':
            total += 1
            name = ele.get('name')
            desc = ele.get('desc')
            Type = ele.get('type')
            manufacturer = ele.get('manufacturer')
            version = ele.get('configVersion')
            ieds.append(("ied_"+str(total),name,Type,manufacturer,desc,version))
    elif event == 'end':
        ele.clear()
# for item in root.iter('{http://www.iec.ch/61850/2003/SCL}IED'):
#     total += 1
#     #print(item.find('title').text)
#     name = item.get('name')
#     desc = item.get('desc')
#     Type = item.get('type')
#     manufacturer = item.get('manufacturer')
#     version = item.get('configVersion')
#     ieds.append(("ied_"+str(total),name,Type,manufacturer,desc,version))

#db.insert(insert_sql,ieds)
print('IED1:',ieds[0])
print("总共%d条数据！"%total)

end = time.clock()

print("程序总运行时间：",end-start,"s")
#----about 2.0 s----#