import sqlite3

def insert(sql,data):
	res = 0
	try:
		conn = sqlite3.connect('BZB.db')
		cursor = conn.cursor()
		cursor.executemany(sql,data)
		res = cursor.rowcount
		conn.commit()
		cursor.close()
		conn.close()
	except Exception as e:
		print(e)
	print("插入数据%d条！"%res)

def select(sql):
	res = None
	try:
		conn = sqlite3.connect('STHB.db')
		cursor = conn.cursor()
		cursor.execute(sql)
		res = cursor.fetchall()
		cursor.close()
		conn.close()
	except Exception as e:
		print(e)
	if res is not None:
		#print('取得数据')
		return res
	else:
		print("无可用数据")
		return None
