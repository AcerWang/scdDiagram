import xml.etree.ElementTree as ET

from DBHelper import DataBase


def resolve(scd_file,target,parent=None):
    '''
        resolve IED from scd file,
        write IED data into database.
    '''
    iterObj = ET.iterparse(scd_file,events=('start','end'))
    
    parent_no, target_no = 0, 0
    data = []
    ns = '{http://www.iec.ch/61850/2003/SCL}'
    value = None
    tb_desc, fields = None, None
    in_Parent = False
    
    db = DataBase()
    try:
        tb_desc = db.tb_desc('select * from %s limit 1'%target)
        fields = ','.join(['?' for e in tb_desc])
        print(tb_desc)
    except Exception as e:
        print(e)

    insert_sql = "insert into %s values(%s)"%(target,fields)
    
    if target == 'LN':
        target = (ns+'LN',ns+'LN0')
    else:
        target = (ns+target,)
    
    # only find target elements
    if parent is None:
        for event,ele in iterObj:
            if event == 'start' and ele.tag in target:
                attr = []
                target_no += 1
                attr.append(target_no)
                for field in tb_desc[1:]:
                    attr.append(ele.get(field))
                data.append(tuple(attr))
                ele.clear()
                # for large data, push into database per 10000 items
                # if target_no%100000 == 0:
                #     db.insert(insert_sql,data)
                #     data = []   
            else:
                ele.clear()
    
    # find both target and parent elements
    else:

        parent_tag = None
        if parent == 'LN':
            parent = (ns+'LN',ns+'LN0')
        else:
            parent = (ns+parent,)

        for event,ele in iterObj:
            if event == 'start':
                if ele.tag in parent:
                    parent_no += 1
                    in_Parent = True
                    parent_tag = ele.tag
                    continue

            elif event == 'end':
                
                # if end of parent element, clean it, save memory
                if ele.tag == parent_tag:
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
                if ele.tag not in target:
                    ele.clear()
                    continue

                # if is target element, retrive data
                target_no += 1
                attrs = []
                attrs.append(target_no)
                attrs.append(parent_no)
                for field in tb_desc[2:]:
                    if field == 'val':
                        attrs.append(value)
                        value = None
                        continue
                    else:
                        attrs.append(ele.get(field))

                data.append(tuple(attrs))
                # for large data, push into database per 10000 items
                # if target_no%100000 == 0:
                #     db.insert(insert_sql,data)
                #     data = []       
    
    db.insert(insert_sql,data)
    db.close_connection()

    print("总共{Target:%d, Parent:%d}条数据！"%(target_no,parent_no))

if __name__ == '__main__':
    import time
    start = time.clock()
    resolve('./scd/BXB.scd','IED')
    end = time.clock()

    print("程序总运行时间：",end-start,"s")
