import json
import xml.etree.ElementTree as ET
import db
import time,sys

start = time.clock()

iterObj = ET.iterparse('scd/HS220.scd',events=('start','end'))

no_ied, no_ap, no_server, no_ld = 0, 0, 0, 0
data = []
ied_name, ap_name, ldInst = None, None, None
sql = 'INSERT INTO IEDTree VALUES (?,?,?,?,?,?)'
ns = '{http://www.iec.ch/61850/2003/SCL}'

for event, ele in iterObj:
    if event == 'start':
        if ele.tag == ns+'DataTypeTemplates':
            break
        if ele.tag == ns+'IED':
            no_ied += 1
            ied_name = ele.get('name')
            continue
        if ele.tag == ns+'AccessPoint':
            no_ap += 1
            ap_name = ele.get('name')
            continue
        if ele.tag == ns+'Server':
            no_server += 1
            continue
        if ele.tag == ns+'LDevice':
            no_ld += 1
            ldInst = ele.get('inst')
            data.append((ied_name,ap_name,no_ap,no_server,ldInst,no_ld))
            continue
    elif event == 'end':
        ele.clear()
        continue

#db = db.DataBase()
#db.insert(sql,data)
#db.close_connection()
print('total records:',no_ld)
end = time.clock()
print('total time:',end-start,'s')