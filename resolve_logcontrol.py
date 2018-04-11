#-*- encoding=utf8 -*-
import db
import xml.etree.ElementTree as ET
import time


start = time.clock()

tree = ET.parse('HS220.scd')
root = tree.getroot()


total,ln0_num = 0,0
insert_sql = "insert into LogControl values(?,?,?,?,?,?,?,?,?)"
data = []
for item in root.iter('{http://www.iec.ch/61850/2003/SCL}LN0'):
    ln0_num += 1
    
    for logc in item.iter('{http://www.iec.ch/61850/2003/SCL}LogControl'):
        total += 1
        name = logc.get('name')
        datSet = logc.get('datSet')
        intgPd = logc.get('intgPd')
        logName = logc.get('logName')
        logEna = logc.get('logEna')
        reasonCode = logc.get('reasonCode')
        desc = logc.get('desc')
        data.append(("log_"+str(total),"ln0_"+str(ln0_num),name,datSet,intgPd,logName,logEna,reasonCode,desc))

db.insert(insert_sql,data)
print("LN0数：%d，LogControl数：%d！"%(ln0_num,total))

end = time.clock()

print("程序总运行时间：",end-start,"s")
#----about 2.0 s----#