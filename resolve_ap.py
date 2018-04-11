#-*- encoding=utf8 -*-
import db
import xml.etree.ElementTree as ET
import time


start = time.clock()

tree = ET.parse('HS220.scd')
root = tree.getroot()

print(root.tag)
insert_sql = "insert into accesspoint values(?,?,?,?,?)"

total,ied_num = 0,0
ap_data = []
for item in root.iter('{http://www.iec.ch/61850/2003/SCL}IED'):
    ied_num += 1
    for ap in item.iter('{http://www.iec.ch/61850/2003/SCL}AccessPoint'):
        total += 1
        name = ap.get('name')
        inst = ap.get('router')
        desc = ap.get('clock')
        ap_data.append(("ap_"+str(total),"ied_"+str(ied_num),name,inst,desc))

db.insert(insert_sql,ap_data)

print("IED数:%d，AccessPoint数：%d！"%(ied_num,total))

end = time.clock()

print("程序总运行时间：",end-start,"s")
#---- about 1.32 s----#