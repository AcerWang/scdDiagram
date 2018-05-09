from pymongo import MongoClient
import xml.etree.ElementTree as ET
import json

ns = '{http://www.iec.ch/61850/2003/SCL}'

conn = MongoClient('localhost',27017)
db = conn.mydb
my_set = db.IEDs

iterObj = ET.iterparse("./scd/HS220.scd",events=('start','end'))
count = 0
for event,ele in iterObj:
    if event == 'start':
        pass

    elif event == 'end':
        if ele.tag == 'IED':
            count += 1
            jobj = json.dumps(ele)         
            my_set.insert(jobj)
            ele.clear()

print('Total:',count)