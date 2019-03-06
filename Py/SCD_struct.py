import json
import xml.etree.ElementTree as ET

from DBHelper import DataBase

def resolve_struct(file_name):
    iterObj = ET.iterparse(file_name,events=('start','end'))

    no_ied, no_ap, no_services, no_server, no_ld = 0, 0, 0, 0, 0
    data = []
    ied_name, ap_name, ldInst = None, None, None
    sql = 'INSERT INTO IEDTree VALUES (?,?,?,?,?,?,?)'
    ns = '{http://www.iec.ch/61850/2003/SCL}'

    for event, ele in iterObj:
        if event == 'start':
            if ele.tag == ns+'DataTypeTemplates':
                break
            if ele.tag == ns+'IED':
                no_ied += 1
                ied_name = ele.get('name')
                continue
            
            if ele.tag == ns+'Services':
                no_services += 1
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
                data.append((ied_name, no_services, ap_name, no_ap, no_server, ldInst, no_ld))
                continue
        elif event == 'end':
            ele.clear()
            continue

    db = DataBase()

    db.insert(sql,data)
    db.close_connection()
    print('total records:',no_ld)

if __name__ == '__main__':
    import time
    start = time.clock()
    resolve_struct('scd/ZTB.scd')
    end = time.clock()
    print('total time:',end-start,'s')
