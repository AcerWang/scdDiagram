#-*- encoding=utf8 -*-
import db
import xml.etree.ElementTree as ET
import time


start = time.clock()

tree = ET.parse('HS220.scd')
root = tree.getroot()


total, ipt_num, ln0_num = 0,0,0
insert_sql = "insert into inputs values(?,?,?,?,?,?,?,?,?,?,?)"
data = []
for item in root.iter('{http://www.iec.ch/61850/2003/SCL}LN0'):
    ln0_num += 1
    
    ipt = item.find('{http://www.iec.ch/61850/2003/SCL}Inputs')
    if ipt is not None:
        ipt_num += 1
        for ref in ipt.iter("{http://www.iec.ch/61850/2003/SCL}ExtRef"):
            total += 1
            iedName = ref.get('iedName')
            prefix = ref.get('prefix')
            doName = ref.get('doName')
            lnInst = ref.get('lnInst')
            lnClass = ref.get('lnClass')
            daName = ref.get('daName')
            intAddr = ref.get('intAddr')
            ldInst = ref.get('ldInst')
            #data.append(("ext_"+str(total),"ipt_"+str(ipt_num),"ln0_"+str(ln0_num),iedName,prefix,doName,lnInst,lnClass,daName,intAddr,ldInst))
            
        

#db.insert(insert_sql,data)
print("LN0数：%d，Inputs数：%d，ExtRef数：%d！"%(ln0_num,ipt_num,total))

end = time.clock()

print("程序总运行时间：",end-start,"s")
#----about 2.0 s----#