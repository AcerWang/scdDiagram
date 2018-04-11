#-*- encoding=utf8 -*-
import db
import xml.etree.ElementTree as ET
import time


start = time.clock()

tree = ET.parse('HS220.scd')
root = tree.getroot()

print(root.tag)
insert_sql = "insert into doi values(?,?,?,?)"

total,ln0_num, ln_num = 0,0,0
data = []
for ln0 in root.iter('{http://www.iec.ch/61850/2003/SCL}LN0'):
        ln0_num += 1
        for ds in ln0.iter('{http://www.iec.ch/61850/2003/SCL}DOI'):
            total += 1
            desc = ds.get('desc')
            name = ds.get('name')
            data.append(("doi_"+str(total),"ln0_"+str(ln0_num),name,desc))

for ln in root.iter('{http://www.iec.ch/61850/2003/SCL}LN'):
        ln_num += 1
        for ds in ln.iter('{http://www.iec.ch/61850/2003/SCL}DOI'):
            total += 1
            desc = ds.get('desc')
            name = ds.get('name')
            data.append(("doi_"+str(total),"ln_"+str(ln_num),name,desc))      

db.insert(insert_sql,data)

print("LN数:%d，LN0数：%d，DOI数：%d！"%(ln_num,ln0_num,total))

end = time.clock()

print("程序总运行时间：",end-start,"s")
#---- about 1.9 s----#