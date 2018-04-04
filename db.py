import pymysql as mysql
'''
   本模块负责处理数据库操作，
   为数据连接层
'''

def insert(sql_insert,args):
    try:
        conn = mysql.connect(host="localhost",port=3306,user="root",passwd="wxy19910101",db="scd",charset="utf8")
        cursor = conn.cursor()
        
        """
        插入数据到数据库中
        """
        cunt = cursor.executemany(sql_insert,args)
        conn.commit()
        """
        关闭游标对象和数据库连接对象
        """
        cursor.close()
        conn.close()
        print("总共插入%d条数据"%cunt)
    except Exception as e:
        print(e)