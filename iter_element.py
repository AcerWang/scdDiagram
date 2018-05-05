#-*- encoding=utf8 -*-
import db
import xml.etree.ElementTree as ET
import time

def resolve(scd_file,element,element_parent=None):
    '''
        resolve IED from scd file,
        write IED data into database.
    '''
    iterObj = ET.iterparse(scd_file,events=('start','end'))

    parent_no, element_no = 0, 0
    data = []
    
    tb_desc = db.tb_desc('select * from %s limit 1'%element)
    print(tb_desc)
    fields = ','.join(['?' for e in tb_desc])
    insert_sql = "insert into %s values(%s)"%(element,fields)
    
    if element_parent is None:
        parent_tag = None
    else:
        parent_tag = '{http://www.iec.ch/61850/2003/SCL}'+element_parent
    element_tag = '{http://www.iec.ch/61850/2003/SCL}'+element

    for event,ele in iterObj:
        if event == 'start':
            if parent_tag and ele.tag == parent_tag:
                parent_no += 1
            if ele.tag ==element_tag:
                element_no += 1
                attrs = []
                for field in tb_desc:
                    if element_parent and field.startswith(str.lower(element_parent)):
                        attrs.append(field+"_"+str(parent_no))
                    elif field.startswith(str.lower(element)):
                        attrs.append(field+"_"+str(element_no))
                    else:
                        attrs.append(ele.get(field))
                data.append(tuple(attrs))
        elif event == 'end':
            ele.clear()
    
    db.insert(insert_sql,data)
    
    print("总共{%d}条数据！"%element_no)

if __name__ == '__main__':
    start = time.clock()
    resolve('ZTB.scd','AccessPoint','IED')
    end = time.clock()

    print("程序总运行时间：",end-start,"s")
