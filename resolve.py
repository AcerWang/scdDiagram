#import db
import xml.etree.ElementTree as ET
import time

start = time.clock()

tree = ET.parse('HS220.scd')
root = tree.getroot()

print(root.tag)
total = 0
for item in root.iter('{http://www.iec.ch/61850/2003/SCL}IED'):
    total += 1
    #print(item.find('title').text)
    print(item.get('desc'))
print("总共%d条数据！"%total)
end = time.clock()

print("程序总运行时间：",end-start,"s")