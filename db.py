import sqlite3


def insert(sql,data):
	res = 0
	try:
		conn = sqlite3.connect('HS220.db')
		cursor = conn.cursor()
		cursor.executemany(sql,data)
		res = cursor.rowcount
		conn.commit()
		cursor.close()
		conn.close()
	except Exception as e:
		print(e)
	print("插入数据%d条！"%res)