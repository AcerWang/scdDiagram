import xml.etree.ElementTree as ET
import time

start = time.clock()
tree = ET.parse('HS220.scd')

root = tree.getroot()

Subnetwork = root.iter('{http://www.iec.ch/61850/2003/SCL}SubNetwork')

for sub in Subnetwork:
    
    print(sub.get('name'))
    
    aps = sub.iter('{http://www.iec.ch/61850/2003/SCL}ConnectedAP')
    for ap in aps:
        print(" "*4+ap.get('iedName'),end=',')
        addr = ap.find('{http://www.iec.ch/61850/2003/SCL}Address')
        
        if addr is None:
            print("")
            continue
        ip = addr.find('{http://www.iec.ch/61850/2003/SCL}P')
        print(ip.text)
end = time.clock()
dur = end-start
print("程序运行时间：%f s"%dur)