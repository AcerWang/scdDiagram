#-*- encoding=utf8 -*-
import db
import xml.etree.ElementTree as ET
import time

def resolve(scd_file,element,element_parent):
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
    
    ns = '{http://www.iec.ch/61850/2003/SCL}'

    for event,ele in iterObj:
        if event == 'start':
            if ele.tag == ns+element_parent:
                parent_no += 1               
        else:
            # if not the target element, clean it, save memory
            if ele.tag != ns+element:
                ele.clear()
                continue
            for e in ele.iter(ns+element):
                attrs = []
                element_no += 1
                for field in tb_desc:
                    # doi for ln ln0 use the second to compare
                    if field.startswith(str.lower(element_parent)) or field.startswith(str.lower(element_parent)[:2]):
                        attrs.append(field+"_"+str(parent_no))
                    elif field.startswith(str.lower(element)):
                        attrs.append(field+"_"+str(element_no))
                    else:
                        attrs.append(e.get(field))
                data.append(tuple(attrs))

    db.insert(insert_sql,data)
    print("总共{%d,%d}条数据！"%(parent_no,element_no))

if __name__ == '__main__':
    start = time.clock()
    resolve('./scd/STHB.scd','DOI','LN0')
    end = time.clock()

    print("程序总运行时间：",end-start,"s")