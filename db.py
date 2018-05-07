import sqlite3

class DataBase(object):

	def __init__(self):
    		self.conn, self.cursor = self.open_connection() 

	def open_connection(self):
		cursor,cursor = None, None
		try:
			conn = sqlite3.connect('test.db')
			cursor = conn.cursor()
		except Exception as e:
			print(e)
			return None
		return conn,cursor

	def close_connection(self):
		try:
			if self.cursor is not None:
				self.cursor.close()
			if self.conn is not None:
				self.conn.close()
		except Exception as e:
			print(e)

	def tb_desc(self,sql):
		desc = []
		try:
			self.cursor.execute(sql)
			desc = self.cursor.description
		except Exception as e:
			print(e)
			return None
		return [e[0] for e in desc]

	def insert(self,sql,data):
		res = 0
		try:
			self.cursor.executemany(sql,data)
			res = self.cursor.rowcount
			self.conn.commit()
		except Exception as e:
			print(e)
		print("插入数据%d条！"%res)

	def select(self,sql):
		res = None
		try:
			self.cursor.execute(sql)
			res = self.cursor.fetchall()
		except Exception as e:
			print(e)
		if res is not None:
			return res
		else:
			print("无可用数据")
			return None
