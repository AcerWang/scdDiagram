#-*- encoding=utf8 -*-
import db
import xml.etree.ElementTree as ET
import time


start = time.clock()

tree = ET.parse('HS220.scd')
root = tree.getroot()

print(root.tag)
insert_sql = "insert into dai values(?,?,?,?)"

total,doi_num = 0,0
data = []
for doi in root.iter('{http://www.iec.ch/61850/2003/SCL}DOI'):
        doi_num += 1
        for dai in doi.iter('{http://www.iec.ch/61850/2003/SCL}DAI'):
            total += 1
            name = dai.get('name')
            value = dai.find('{http://www.iec.ch/61850/2003/SCL}Val')
            if value is not None:
                value = value.text
            data.append(("dai_"+str(total),"doi_"+str(doi_num),name,value))
        

db.insert(insert_sql,data)

print("DOI数:%d，DAI数：%d！"%(doi_num,total))

end = time.clock()

print("程序总运行时间：",end-start,"s")
#---- about 2.2 s----#