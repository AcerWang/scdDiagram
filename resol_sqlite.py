import sqlite3
import xml.etree.ElementTree as ET
import time


start = time.clock()

tree = ET.parse('HS220.scd')
root = tree.getroot()

print(root.tag)
total = 0
insert_sql = "insert into ied values"

items = []
for item in root.iter('{http://www.iec.ch/61850/2003/SCL}IED'):
	total += 1
	#print(item.find('title').text)
	name = item.get('name')
	desc = item.get('desc')
	Type = item.get('type')
	manufacturer = item.get('manufacturer')
	version = item.get('configVersion')
	items.append((total,name,desc,Type,manufacturer,version))
	#insert_sql += '(%s,"%s","%s","%s","%s","%s"),'%(total,name,desc,Type,manufacturer,version)

#print(insert_sql[0:100])

try:
	conn = sqlite3.connect('HS220.db')
	cursor = conn.cursor()
	cursor.execute('''CREATE TABLE ied(
						id int auto_increment primary key,
						name varchar(40), 
						desc varchar(80),
						type varchar(20), 
						manufacturer varchar(40),
						configVersion varchar(20))''')

	cursor.executemany("insert into ied values(?,?,?,?,?,?)",items)
	conn.commit()
	cursor.close()
	conn.close()
except Exception as e:
	print(e)

#print("总共%d条数据！"%total)

end = time.clock()

print("程序总运行时间：",end-start,"s")