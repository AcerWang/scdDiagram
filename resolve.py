import util
import xml.etree.ElementTree as ET
import time


start = time.clock()

tree = ET.parse('HS220.scd')
root = tree.getroot()

print(root.tag)
total = 0


data = util.getTags(root.iter('{http://www.iec.ch/61850/2003/SCL}IED'),'name','desc','type','manufacturer','configVersion')

util.createTable('''
	DROP TABLE IF EXISTS subNetwork
''')

util.createTable('''CREATE TABLE subNetwork(
							id int auto_increment primary key,
							name varchar(10), 
							desc varchar(10),
							type varchar(10), 
							iedName varchar(20),
							apName varchar(10),
							IP varchar(20),
							IP_SUBNET varchar(20),
							IP_GATEWAY varchar(20)
							)''')

util.writeDB('insert into subNetwork(name,desc,type,iedName,apName,IP,IP_SUBNET,IP_GATEWAY) values(?,?,?,?,?)',data)

end = time.clock()

print("程序总运行时间：",end-start,"s")
