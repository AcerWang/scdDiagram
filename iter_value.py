#-*- encoding=utf8 -*-
import db
import xml.etree.ElementTree as ET
import time

def resolve(scd_file, element, element_parent=None):
    '''
        resolve IED from scd file,
        write IED data into database.
    '''
    iterObj = ET.iterparse(scd_file,events=('start','end'))

    parent_no, element_no = 0, 0
    data = []

    for event,ele in iterObj:
        if event == 'start':
            if element_parent and ele.tag == element_parent:
                parent_no += 1
                for t in ele.iterfind(element):
                    element_no += 1
                    data.append(t.text)
        elif event == 'end':
            ele.clear()
    print(data)
    print("总共{%d}条数据！"%element_no)

if __name__ == '__main__':
    start = time.clock()
    resolve('news.xml','title','item')
    end = time.clock()

    print("程序总运行时间：",end-start,"s")
