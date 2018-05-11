#-*- encoding=utf8 -*-
from db import DataBase
import xml.etree.ElementTree as ET
import time

def resolve(scd_file,target,parent=None):
    '''
        resolve IED from scd file,
        write IED data into database.
    '''
    iterObj = ET.iterparse(scd_file,events=('start','end'))
    
    parent_no, target_no = 0, 0
    data = []
    #value = None
    tb_desc, fields = None, None
    #in_Parent = False
    
    db = DataBase()
    
    try:
        tb_desc = db.tb_desc('select * from %s limit 1'%target)
        fields = ','.join(['?' for e in tb_desc])
        print(fields)
    except Exception as e:
        print(e)

    insert_sql = "insert into %s values(%s)"%(target,fields)
    
    ns = '{http://www.iec.ch/61850/2003/SCL}'
    if parent is None:
        for event,ele in iterObj:
            if event == 'start' and ele.tag == ns+target:
                attr = []
                target_no += 1
                attr.append(target_no)
                for field in tb_desc[1:]:
                    attr.append(ele.get(field))
                data.append(tuple(attr))
            else:
                ele.clear()
        db.insert(insert_sql,data)
        db.close_connection()
    print(data[-1])
    print("总共{%d}条数据！"%(target_no))

if __name__ == '__main__':
    start = time.clock()
    resolve('./scd/HS220.scd','IED')
    end = time.clock()

    print("程序总运行时间：",end-start,"s")