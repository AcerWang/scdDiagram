#-*- encoding=utf8 -*-
from db import DataBase
import xml.etree.ElementTree as ET
import time

def resolve(scd_file,element,*element_parents):
    '''
        resolve IED from scd file,
        write IED data into database.
    '''
    iterObj = ET.iterparse(scd_file,events=('start','end'))
    
    parent_no, element_no = 0, 0
    data = []
    value, target, parent = None, None, None
    tb_name, tb_desc, fields = None, None, None

    if '/' in element:
        element = element.split('/')
        target = element[0]
    else:
        target = element
        element = [element,]
    
    db = DataBase()
    
    try:
        tb_desc = db.tb_desc('select * from %s limit 1'%target)
        print(tb_desc)
        fields = ','.join(['?' for e in tb_desc])
        tb_name = target
    except Exception as e:
        # for LN0
        tb_desc = db.tb_desc('select * from %s limit 1'%target[:2])
        print(tb_desc)
        fields = ','.join(['?' for e in tb_desc])
        tb_name = target[:2]

    insert_sql = "insert into %s values(%s)"%(tb_name,fields)
    
    ns = '{http://www.iec.ch/61850/2003/SCL}'
    element = [ ns+e for e in element ]
    element_parents = [ ns+e for e in element_parents ]
    parent = element_parents[0]
    in_Parent = False
    
    for event,ele in iterObj:
        if event == 'start':
            if ele.tag in element_parents:
                parent = ele.tag
                in_Parent = True
                parent_no += 1
                continue

        elif event == 'end':
            
            # if end of parent element, clean it, save memory
            if ele.tag == parent:
                ele.clear()
                in_Parent = False
                continue

            if not in_Parent:
                ele.clear()
                continue
            
            # Val for DAI
            if ele.tag == ns+'Val':
                value = ele.text
                ele.clear()
                continue
            
            # if not the target element, clean it, save memory
            if ele.tag not in element:
                ele.clear()
                continue
            
            # if is target, set `target`= ele.tag
            target = ele.tag

            # if is target element, retrive data
            for e in ele.iter(target):
                target = target[34:]
                
                attrs = []
                element_no += 1
                for field in tb_desc:
                    if field == str.lower(parent[34:]) or field == str.lower(parent[34:-1]):
                        attrs.append(parent[34:]+"_"+str(parent_no))
                    # in case of that doi for ln, ln0 
                    elif field == str.lower(target) or field == str.lower(target[:-1]):
                        attrs.append(target+"_"+str(element_no))
                    else:
                        if field == 'val':
                            attrs.append(value)
                            value = None
                            continue
                        attrs.append(e.get(field))
                data.append(tuple(attrs))
                #for large data, push into database per 10000 items
                if element_no%10000 == 0:
                    db.insert(insert_sql,data)
                    data = []       
    db.insert(insert_sql,data)
    db.close_connection()

    print("总共{%d,%d}条数据！"%(parent_no,element_no))

if __name__ == '__main__':
    start = time.clock()
    resolve('./scd/HSB.scd','DAI','DOI')
    end = time.clock()

    print("程序总运行时间：",end-start,"s")